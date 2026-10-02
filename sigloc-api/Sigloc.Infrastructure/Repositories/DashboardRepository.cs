using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private const string Arrow = " \u2192 "; // " → "

    private readonly IDbContextFactory<SiglocDbContext> _contextFactory;

    public DashboardRepository(IDbContextFactory<SiglocDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardKpis> GetKpisAsync(
        Guid contractorId,
        DateTimeOffset monthStartUtc,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var unassignedSegments = await db.RouteSegments
            .AsNoTracking()
            .CountAsync(s => s.ContractorId == contractorId && s.Status == SegmentStatus.Available, cancellationToken);

        var activeAuctions = await (
            from auction in db.Auctions.AsNoTracking()
            join route in db.ConsolidatedRoutes.AsNoTracking() on auction.RouteId equals route.Id
            where route.ContractorId == contractorId && auction.Status == AuctionStatus.Open
            select auction.Id)
            .CountAsync(cancellationToken);

        var inTransitTrips = await (
            from trip in db.Trips.AsNoTracking()
            join route in db.ConsolidatedRoutes.AsNoTracking() on trip.RouteId equals route.Id
            where route.ContractorId == contractorId && trip.Status == TripStatus.InTransit
            select trip.Id)
            .CountAsync(cancellationToken);

        var blockedOverbookings = await db.BlockedBidAttempts
            .AsNoTracking()
            .CountAsync(a => a.ContractorId == contractorId && a.AttemptedAt >= monthStartUtc, cancellationToken);

        return new DashboardKpis(unassignedSegments, activeAuctions, inTransitTrips, blockedOverbookings);
    }

    public async Task<IReadOnlyList<TripOccupation>> GetInTransitOccupationsAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await (
            from trip in db.Trips.AsNoTracking()
            join route in db.ConsolidatedRoutes.AsNoTracking() on trip.RouteId equals route.Id
            join vehicle in db.Vehicles.AsNoTracking() on trip.VehicleId equals vehicle.Id
            where route.ContractorId == contractorId && trip.Status == TripStatus.InTransit
            select new
            {
                trip.RouteId,
                route.TotalWeightKg,
                route.TotalVolumeM3,
                vehicle.CapacityWeight,
                vehicle.CapacityVolume
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Array.Empty<TripOccupation>();
        }

        // Segment counts per route in one round-trip; used for the Continuous Move indicator.
        var routeIds = rows.Select(r => r.RouteId).Distinct().ToList();
        var segmentCounts = await db.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .GroupBy(s => s.RouteId!.Value)
            .Select(g => new { RouteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);

        return rows
            .Select(r => new TripOccupation(
                r.TotalWeightKg,
                (double)r.CapacityWeight,
                r.TotalVolumeM3,
                (double)r.CapacityVolume,
                segmentCounts.TryGetValue(r.RouteId, out var count) ? count : 0))
            .ToList();
    }

    public async Task<IReadOnlyList<AuctionCostDeviation>> GetCostDeviationsAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Active or recently-closed auctions with at least one active bid.
        var auctions = await (
            from auction in db.Auctions.AsNoTracking()
            join route in db.ConsolidatedRoutes.AsNoTracking() on auction.RouteId equals route.Id
            where route.ContractorId == contractorId
                && (auction.Status == AuctionStatus.Open || auction.Status == AuctionStatus.Closed)
            select new
            {
                AuctionId = auction.Id,
                RouteId = route.Id,
                route.ConsolidatedCeiling
            })
            .ToListAsync(cancellationToken);

        if (auctions.Count == 0)
        {
            return Array.Empty<AuctionCostDeviation>();
        }

        var auctionIds = auctions.Select(a => a.AuctionId).ToList();

        var bestBids = await db.Bids
            .AsNoTracking()
            .Where(b => auctionIds.Contains(b.AuctionId) && b.Status != BidStatus.Withdrawn)
            .GroupBy(b => b.AuctionId)
            .Select(g => new { AuctionId = g.Key, BestBid = g.Min(b => b.TotalValue) })
            .ToDictionaryAsync(x => x.AuctionId, x => x.BestBid, cancellationToken);

        var itineraries = await BuildItinerariesAsync(
            db,
            auctions.ToDictionary(a => a.AuctionId, a => a.RouteId),
            cancellationToken);

        var result = new List<AuctionCostDeviation>();
        foreach (var a in auctions)
        {
            if (!bestBids.TryGetValue(a.AuctionId, out var bestBid))
            {
                continue; // No active bid → no deviation to show.
            }

            itineraries.TryGetValue(a.AuctionId, out var itinerary);
            result.Add(new AuctionCostDeviation(
                RouteId: a.RouteId,
                Itinerary: itinerary ?? string.Empty,
                TargetBudget: a.ConsolidatedCeiling,
                CurrentBestBid: bestBid));
        }

        return result;
    }

    public async Task<IReadOnlyList<TripSlaMilestone>> GetSlaMilestonesAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var trips = await (
            from trip in db.Trips.AsNoTracking()
            join route in db.ConsolidatedRoutes.AsNoTracking() on trip.RouteId equals route.Id
            where route.ContractorId == contractorId && trip.Status == TripStatus.InTransit
            join monitoring in db.TripMonitorings.AsNoTracking() on trip.Id equals monitoring.TripId into mon
            from monitoring in mon.DefaultIfEmpty()
            select new
            {
                trip.Id,
                trip.RouteId,
                NextStopKind = db.TripStopActions.Where(a => monitoring != null && a.TripStopId == monitoring.NextStopId && !a.IsCompleted)
                    .OrderBy(a => a.Deadline).Select(a => (StopActionKind?)a.Kind).FirstOrDefault(),
                NextStopDeadline = monitoring == null ? null : monitoring.NextStopDeadline,
                LastCalculatedEta = (DateTimeOffset?)(monitoring != null ? monitoring.LastCalculatedEta : (DateTimeOffset?)null)
            })
            .ToListAsync(cancellationToken);

        if (trips.Count == 0)
        {
            return Array.Empty<TripSlaMilestone>();
        }

        var routeIds = trips.Select(t => t.RouteId).Distinct().ToList();

        // Earliest-deadline pending segment per route defines the next milestone.
        var segments = await db.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .Select(s => new
            {
                RouteId = s.RouteId!.Value,
                s.Status,
                s.OriginAddress,
                s.DestinationAddress,
                s.PickupDeadline,
                s.DeliveryDeadline
            })
            .ToListAsync(cancellationToken);

        var segmentsByRoute = segments
            .GroupBy(s => s.RouteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TripSlaMilestone>();
        foreach (var trip in trips)
        {
            if (!segmentsByRoute.TryGetValue(trip.RouteId, out var routeSegments) || routeSegments.Count == 0)
            {
                continue;
            }

            var ordered = routeSegments.OrderBy(s => s.PickupDeadline).ToList();
            var origin = CityName(ordered.First().OriginAddress);
            var destination = CityName(ordered.Last().DestinationAddress);
            var itinerary = $"{origin}{Arrow}{destination}";

            // The next open leg drives the milestone: not yet picked up → COLETA, else ENTREGA.
            var nextPickup = ordered.FirstOrDefault(s => s.Status == SegmentStatus.Routed || s.Status == SegmentStatus.InTransit);
            var isPickup = nextPickup != null && nextPickup.Status == SegmentStatus.Routed;
            var deadline = isPickup
                ? nextPickup!.PickupDeadline
                : ordered.Min(s => s.DeliveryDeadline);
            var milestoneType = isPickup ? "COLETA" : "ENTREGA";

            result.Add(new TripSlaMilestone(
                ReferenceCode: ShortCode(trip.Id),
                Itinerary: itinerary,
                MilestoneType: trip.NextStopKind.HasValue ? (trip.NextStopKind == StopActionKind.Pickup ? "COLETA" : "ENTREGA") : milestoneType,
                SlaDeadline: trip.NextStopDeadline ?? deadline,
                LastCalculatedEta: trip.LastCalculatedEta));
        }

        return result;
    }

    public async Task<CarrierDashboardKpis> GetCarrierKpisAsync(
        Guid carrierId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var availableVehicles = await db.Vehicles
            .AsNoTracking()
            .CountAsync(v => v.TransportadoraId == carrierId && v.Status == OperationalStatus.LIVRE, cancellationToken);

        var activeBids = await (
            from bid in db.Bids.AsNoTracking()
            join auction in db.Auctions.AsNoTracking() on bid.AuctionId equals auction.Id
            where bid.CarrierId == carrierId
                && bid.Status != BidStatus.Withdrawn
                && auction.Status == AuctionStatus.Open
            select bid.Id)
            .CountAsync(cancellationToken);

        var inTransitTrips = await db.Trips
            .AsNoTracking()
            .CountAsync(t => t.CarrierId == carrierId && t.Status == TripStatus.InTransit, cancellationToken);

        return new CarrierDashboardKpis(availableVehicles, activeBids, inTransitTrips);
    }

    public async Task<CarrierPerformanceInputs> GetCarrierPerformanceInputsAsync(
        Guid carrierId,
        DateTimeOffset monthStartUtc,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var totalVehicles = await db.Vehicles
            .AsNoTracking()
            .CountAsync(v => v.TransportadoraId == carrierId, cancellationToken);

        // "Fleet in operation": every truck currently not free (in transit or maintenance).
        var busyVehicles = await db.Vehicles
            .AsNoTracking()
            .CountAsync(v => v.TransportadoraId == carrierId && v.Status != OperationalStatus.LIVRE, cancellationToken);

        var submittedBids = await db.Bids
            .AsNoTracking()
            .CountAsync(b => b.CarrierId == carrierId && b.SubmittedAt >= monthStartUtc, cancellationToken);

        var wonBids = await db.Bids
            .AsNoTracking()
            .CountAsync(b => b.CarrierId == carrierId && b.Status == BidStatus.Winner && b.SubmittedAt >= monthStartUtc, cancellationToken);

        return new CarrierPerformanceInputs(totalVehicles, busyVehicles, submittedBids, wonBids);
    }

    public async Task<IReadOnlyList<TripOccupation>> GetInTransitOccupationsByCarrierAsync(
        Guid carrierId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await (
            from trip in db.Trips.AsNoTracking()
            join route in db.ConsolidatedRoutes.AsNoTracking() on trip.RouteId equals route.Id
            join vehicle in db.Vehicles.AsNoTracking() on trip.VehicleId equals vehicle.Id
            where trip.CarrierId == carrierId && trip.Status == TripStatus.InTransit
            select new
            {
                trip.RouteId,
                route.TotalWeightKg,
                route.TotalVolumeM3,
                vehicle.CapacityWeight,
                vehicle.CapacityVolume
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Array.Empty<TripOccupation>();
        }

        var routeIds = rows.Select(r => r.RouteId).Distinct().ToList();
        var segmentCounts = await db.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .GroupBy(s => s.RouteId!.Value)
            .Select(g => new { RouteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);

        return rows
            .Select(r => new TripOccupation(
                r.TotalWeightKg,
                (double)r.CapacityWeight,
                r.TotalVolumeM3,
                (double)r.CapacityVolume,
                segmentCounts.TryGetValue(r.RouteId, out var count) ? count : 0))
            .ToList();
    }

    public async Task<IReadOnlyList<ActiveBidDispute>> GetActiveBidDisputesAsync(
        Guid carrierId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // The carrier's active bids in open auctions (best/lowest bid per auction if several exist).
        var myBids = await (
            from bid in db.Bids.AsNoTracking()
            join auction in db.Auctions.AsNoTracking() on bid.AuctionId equals auction.Id
            where bid.CarrierId == carrierId
                && bid.Status != BidStatus.Withdrawn
                && auction.Status == AuctionStatus.Open
            select new { bid.AuctionId, auction.RouteId, bid.TotalValue })
            .ToListAsync(cancellationToken);

        if (myBids.Count == 0)
        {
            return Array.Empty<ActiveBidDispute>();
        }

        var myBestBidByAuction = myBids
            .GroupBy(b => b.AuctionId)
            .ToDictionary(g => g.Key, g => g.Min(b => b.TotalValue));

        var auctionRoutes = myBids
            .GroupBy(b => b.AuctionId)
            .ToDictionary(g => g.Key, g => g.First().RouteId);

        var auctionIds = myBestBidByAuction.Keys.ToList();

        // Current leader (lowest total across every active bid) for each auction.
        var leaderByAuction = await db.Bids
            .AsNoTracking()
            .Where(b => auctionIds.Contains(b.AuctionId) && b.Status != BidStatus.Withdrawn)
            .GroupBy(b => b.AuctionId)
            .Select(g => new { AuctionId = g.Key, Leader = g.Min(b => b.TotalValue) })
            .ToDictionaryAsync(x => x.AuctionId, x => x.Leader, cancellationToken);

        var itineraries = await BuildItinerariesAsync(db, auctionRoutes, cancellationToken);

        var result = new List<ActiveBidDispute>();
        foreach (var auctionId in auctionIds)
        {
            var myBid = myBestBidByAuction[auctionId];
            var leader = leaderByAuction.TryGetValue(auctionId, out var l) ? l : myBid;
            itineraries.TryGetValue(auctionId, out var itinerary);

            result.Add(new ActiveBidDispute(
                RouteId: auctionRoutes[auctionId],
                Itinerary: itinerary ?? string.Empty,
                MyBidAmount: myBid,
                LeaderBidAmount: leader));
        }

        return result;
    }

    public async Task<IReadOnlyList<CarrierSlaMilestone>> GetCarrierSlaMilestonesAsync(
        Guid carrierId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var trips = await (
            from trip in db.Trips.AsNoTracking()
            join vehicle in db.Vehicles.AsNoTracking() on trip.VehicleId equals vehicle.Id
            where trip.CarrierId == carrierId && trip.Status == TripStatus.InTransit
            join monitoring in db.TripMonitorings.AsNoTracking() on trip.Id equals monitoring.TripId into mon
            from monitoring in mon.DefaultIfEmpty()
            select new
            {
                trip.Id,
                trip.RouteId,
                vehicle.Plate,
                NextStopKind = db.TripStopActions.Where(a => monitoring != null && a.TripStopId == monitoring.NextStopId && !a.IsCompleted)
                    .OrderBy(a => a.Deadline).Select(a => (StopActionKind?)a.Kind).FirstOrDefault(),
                NextStopDeadline = monitoring == null ? null : monitoring.NextStopDeadline,
                LastCalculatedEta = (DateTimeOffset?)(monitoring != null ? monitoring.LastCalculatedEta : (DateTimeOffset?)null)
            })
            .ToListAsync(cancellationToken);

        if (trips.Count == 0)
        {
            return Array.Empty<CarrierSlaMilestone>();
        }

        var routeIds = trips.Select(t => t.RouteId).Distinct().ToList();

        var segments = await db.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .Select(s => new
            {
                RouteId = s.RouteId!.Value,
                s.Status,
                s.PickupDeadline,
                s.DeliveryDeadline
            })
            .ToListAsync(cancellationToken);

        var segmentsByRoute = segments
            .GroupBy(s => s.RouteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<CarrierSlaMilestone>();
        foreach (var trip in trips)
        {
            if (!segmentsByRoute.TryGetValue(trip.RouteId, out var routeSegments) || routeSegments.Count == 0)
            {
                continue;
            }

            var ordered = routeSegments.OrderBy(s => s.PickupDeadline).ToList();

            // The next open leg drives the milestone: not yet picked up → COLETA, else ENTREGA.
            var nextPickup = ordered.FirstOrDefault(s => s.Status == SegmentStatus.Routed || s.Status == SegmentStatus.InTransit);
            var isPickup = nextPickup != null && nextPickup.Status == SegmentStatus.Routed;
            var deadline = isPickup
                ? nextPickup!.PickupDeadline
                : ordered.Min(s => s.DeliveryDeadline);
            var milestoneType = isPickup ? "COLETA" : "ENTREGA";

            result.Add(new CarrierSlaMilestone(
                VehiclePlate: trip.Plate,
                ReferenceCode: ShortCode(trip.Id),
                MilestoneType: trip.NextStopKind.HasValue ? (trip.NextStopKind == StopActionKind.Pickup ? "COLETA" : "ENTREGA") : milestoneType,
                SlaDeadline: trip.NextStopDeadline ?? deadline,
                LastCalculatedEta: trip.LastCalculatedEta));
        }

        return result;
    }

    private static async Task<Dictionary<Guid, string>> BuildItinerariesAsync(
        SiglocDbContext db,
        IReadOnlyDictionary<Guid, Guid> auctionToRoute,
        CancellationToken cancellationToken)
    {
        var routeIds = auctionToRoute.Values.Distinct().ToList();

        var segments = await db.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .Select(s => new { RouteId = s.RouteId!.Value, s.OriginAddress, s.DestinationAddress, s.PickupDeadline })
            .ToListAsync(cancellationToken);

        var byRoute = segments
            .GroupBy(s => s.RouteId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ordered = g.OrderBy(s => s.PickupDeadline).ToList();
                    var origin = CityName(ordered.First().OriginAddress);
                    var destination = CityName(ordered.Last().DestinationAddress);
                    return $"{origin}{Arrow}{destination}";
                });

        var result = new Dictionary<Guid, string>();
        foreach (var (auctionId, routeId) in auctionToRoute)
        {
            if (byRoute.TryGetValue(routeId, out var itinerary))
            {
                result[auctionId] = itinerary;
            }
        }

        return result;
    }

    private static string CityName(string address)
    {
        var comma = address.IndexOf(',');
        return (comma >= 0 ? address[..comma] : address).Trim();
    }

    private static string ShortCode(Guid id)
        => "TRP-" + id.ToString("N")[..6].ToUpperInvariant();
}
