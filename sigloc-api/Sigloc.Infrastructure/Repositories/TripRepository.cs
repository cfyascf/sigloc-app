using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly SiglocDbContext _dbContext;

    public TripRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.Trips.AddAsync(trip, cancellationToken);
    }

    public async Task<IReadOnlyList<TripScheduleWindow>> GetActiveWindowsForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        // Routes still occupied by the vehicle: trips that are not delivered nor cancelled.
        var windows = await (
            from trip in _dbContext.Trips.AsNoTracking()
            where trip.VehicleId == vehicleId
                && trip.Status != TripStatus.Delivered
                && trip.Status != TripStatus.Cancelled
            join segment in _dbContext.RouteSegments.AsNoTracking()
                on trip.RouteId equals segment.RouteId
            group segment by trip.RouteId into g
            select new TripScheduleWindow(
                g.Min(s => s.PickupDeadline),
                g.Max(s => s.DeliveryDeadline)))
            .ToListAsync(cancellationToken);

        return windows;
    }

    public async Task<VehicleDelivery?> GetLastDeliveryUntilAsync(
        Guid vehicleId,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        // Delivered trips count too: their last delivery is where the vehicle was left.
        return await (
            from trip in _dbContext.Trips.AsNoTracking()
            where trip.VehicleId == vehicleId && trip.Status != TripStatus.Cancelled
            join segment in _dbContext.RouteSegments.AsNoTracking()
                on trip.RouteId equals segment.RouteId
            where segment.DeliveryDeadline <= until
            orderby segment.DeliveryDeadline descending
            select new VehicleDelivery(
                segment.DeliveryDeadline,
                segment.DestinationCoordinate,
                segment.DestinationCity ?? segment.DestinationAddress))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> CountActiveByCarrierForContractorAsync(
        Guid contractorId,
        IReadOnlyCollection<Guid> carrierIds,
        CancellationToken cancellationToken = default)
    {
        if (carrierIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        // The contractor owns the trip through its consolidated route.
        return await (
            from trip in _dbContext.Trips.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking() on trip.RouteId equals route.Id
            where route.ContractorId == contractorId
                && carrierIds.Contains(trip.CarrierId)
                && (trip.Status == TripStatus.AwaitingPickup || trip.Status == TripStatus.InTransit)
            group trip by trip.CarrierId into g
            select new { CarrierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CarrierId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> CountActiveLanesByContractorForCarrierAsync(
        Guid carrierId,
        IReadOnlyCollection<Guid> contractorIds,
        CancellationToken cancellationToken = default)
    {
        if (contractorIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var lanes = await (
            from trip in _dbContext.Trips.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking() on trip.RouteId equals route.Id
            join segment in _dbContext.RouteSegments.AsNoTracking() on route.Id equals segment.RouteId
            where trip.CarrierId == carrierId
                && contractorIds.Contains(route.ContractorId)
                && (trip.Status == TripStatus.AwaitingPickup || trip.Status == TripStatus.InTransit)
            select new { route.ContractorId, segment.OriginAddress, segment.DestinationAddress })
            .Distinct()
            .ToListAsync(cancellationToken);

        // Addresses are compared case-insensitively so "Curitiba, PR" and "curitiba, pr" are one lane.
        return lanes
            .GroupBy(l => l.ContractorId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => (l.OriginAddress.Trim().ToLowerInvariant(), l.DestinationAddress.Trim().ToLowerInvariant()))
                    .Distinct()
                    .Count());
    }
}
