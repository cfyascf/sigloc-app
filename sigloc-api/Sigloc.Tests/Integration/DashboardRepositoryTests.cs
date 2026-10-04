using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class DashboardRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();
    private readonly Guid _carrierId = Guid.NewGuid();

    public DashboardRepositoryTests()
    {
        // FK parents required by Trips/Bids (SQLite enforces FKs).
        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        ctx.Carriers.Add(TestData.Carrier(_carrierId));
        ctx.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private DashboardRepository Repo() => new(new Factory(_db));

    private sealed class Factory : IDbContextFactory<SiglocDbContext>
    {
        private readonly SqliteDatabase _db;
        public Factory(SqliteDatabase db) => _db = db;
        public SiglocDbContext CreateDbContext() => _db.CreateContext();
    }

    private static ConsolidatedRoute Route(
        Guid contractorId,
        RouteStatus status = RouteStatus.Planned,
        double weight = 1000,
        double volume = 40,
        decimal ceiling = 5000) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        Status = status,
        TotalWeightKg = weight,
        TotalVolumeM3 = volume,
        ConsolidatedCeiling = ceiling,
    };

    private static Auction Auction(Guid routeId, AuctionStatus status = AuctionStatus.Open) => new()
    {
        Id = Guid.NewGuid(),
        RouteId = routeId,
        OpenedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        Status = status,
    };

    private static RouteSegment Segment(
        Guid contractorId,
        Guid? routeId,
        SegmentStatus status = SegmentStatus.Available,
        string origin = "Curitiba, PR",
        string destination = "São Paulo, SP",
        DateTimeOffset? pickup = null,
        DateTimeOffset? delivery = null) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        RouteId = routeId,
        OriginAddress = origin,
        DestinationAddress = destination,
        OriginCoordinate = "-49.27,-25.42",
        DestinationCoordinate = "-46.63,-23.55",
        PickupDeadline = pickup ?? DateTimeOffset.UtcNow.AddDays(1),
        DeliveryDeadline = delivery ?? DateTimeOffset.UtcNow.AddDays(2),
        Status = status,
    };

    private static Bid Bid(
        Guid auctionId,
        Guid carrierId,
        Guid vehicleId,
        decimal total,
        BidStatus status = BidStatus.Pending,
        DateTimeOffset? submittedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        AuctionId = auctionId,
        CarrierId = carrierId,
        VehicleId = vehicleId,
        NetFreightValue = total,
        TollValue = 0,
        TotalValue = total,
        SubmittedAt = submittedAt ?? DateTimeOffset.UtcNow,
        Status = status,
    };

    private static Trip Trip(
        Guid routeId,
        Guid auctionId,
        Guid carrierId,
        Guid vehicleId,
        TripStatus status = TripStatus.InTransit) => new()
    {
        Id = Guid.NewGuid(),
        RouteId = routeId,
        AuctionId = auctionId,
        CarrierId = carrierId,
        VehicleId = vehicleId,
        BidId = Guid.NewGuid(),
        AgreedValue = 1000,
        Status = status,
    };

    // TestData.Vehicle leaves Id unset (EF generates it on Add), so assign one up front
    // to make vehicle.Id usable as an FK target before the entity is tracked.
    private Vehicle Vehicle(
        string plate = "ABC1D23",
        OperationalStatus status = OperationalStatus.LIVRE)
    {
        var vehicle = TestData.Vehicle(_carrierId, plate: plate, status: status);
        vehicle.Id = Guid.NewGuid();
        return vehicle;
    }

    // ---------------------------------------------------------------- GetKpisAsync

    [Fact]
    public async Task GetKpisAsync_counts_available_segments_open_auctions_and_in_transit_trips()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId);
        var openAuctionRoute = Route(_contractorId, RouteStatus.InAuction);
        var inTransitRoute = Route(_contractorId, RouteStatus.InTransit);
        var vehicle = Vehicle();
        ctx.ConsolidatedRoutes.AddRange(route, openAuctionRoute, inTransitRoute);
        ctx.Vehicles.Add(vehicle);
        var openAuction = Auction(openAuctionRoute.Id, AuctionStatus.Open);
        var tripAuction = Auction(inTransitRoute.Id, AuctionStatus.Closed);
        ctx.RouteSegments.AddRange(
            Segment(_contractorId, null, SegmentStatus.Available),
            Segment(_contractorId, null, SegmentStatus.Available),
            Segment(_contractorId, null, SegmentStatus.Routed));
        ctx.Auctions.AddRange(openAuction, Auction(route.Id, AuctionStatus.Closed), tripAuction);
        ctx.Trips.Add(Trip(inTransitRoute.Id, tripAuction.Id, _carrierId, vehicle.Id, TripStatus.InTransit));
        await ctx.SaveChangesAsync();

        var kpis = await Repo().GetKpisAsync(_contractorId);

        kpis.UnassignedSegments.Should().Be(2);
        kpis.ActiveAuctions.Should().Be(1);
        kpis.InTransitTrips.Should().Be(1);
    }

    [Fact]
    public async Task GetKpisAsync_scopes_by_contractor_and_returns_zeros_when_empty()
    {
        var kpis = await Repo().GetKpisAsync(Guid.NewGuid());

        kpis.UnassignedSegments.Should().Be(0);
        kpis.ActiveAuctions.Should().Be(0);
        kpis.InTransitTrips.Should().Be(0);
    }

    // ------------------------------------------------ GetInTransitOccupationsAsync

    [Fact]
    public async Task GetInTransitOccupationsAsync_returns_occupation_and_segment_count()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, RouteStatus.InTransit, weight: 10000, volume: 40);
        var auction = Auction(route.Id);
        var vehicle = Vehicle();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.Trips.Add(Trip(route.Id, auction.Id, _carrierId, vehicle.Id, TripStatus.InTransit));
        ctx.RouteSegments.AddRange(
            Segment(_contractorId, route.Id, SegmentStatus.InTransit),
            Segment(_contractorId, route.Id, SegmentStatus.InTransit));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetInTransitOccupationsAsync(_contractorId);

        result.Should().ContainSingle();
        var row = result[0];
        row.RouteWeightKg.Should().Be(10000);
        row.VehicleWeightCapacity.Should().Be(20000);
        row.RouteVolumeM3.Should().Be(40);
        row.VehicleVolumeCapacity.Should().Be(80);
        row.SegmentCount.Should().Be(2);
    }

    [Fact]
    public async Task GetInTransitOccupationsAsync_returns_empty_when_no_in_transit_trips()
    {
        var result = await Repo().GetInTransitOccupationsAsync(_contractorId);
        result.Should().BeEmpty();
    }

    // ------------------------------------------------------- GetCostDeviationsAsync

    [Fact]
    public async Task GetCostDeviationsAsync_returns_best_bid_and_itinerary_for_bidded_auctions()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, ceiling: 8000);
        var auction = Auction(route.Id, AuctionStatus.Open);
        var vehicle = Vehicle();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id, origin: "Curitiba, PR", destination: "São Paulo, SP"));
        ctx.Bids.AddRange(
            Bid(auction.Id, _carrierId, vehicle.Id, 7000, BidStatus.Pending),
            Bid(auction.Id, _carrierId, vehicle.Id, 6500, BidStatus.Winning),
            Bid(auction.Id, _carrierId, vehicle.Id, 100, BidStatus.Withdrawn));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetCostDeviationsAsync(_contractorId);

        result.Should().ContainSingle();
        result[0].RouteId.Should().Be(route.Id);
        result[0].TargetBudget.Should().Be(8000);
        result[0].CurrentBestBid.Should().Be(6500);
        result[0].Itinerary.Should().Be("Curitiba \u2192 São Paulo");
    }

    [Fact]
    public async Task GetCostDeviationsAsync_skips_auctions_without_active_bids_and_returns_empty_when_none()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId);
        var auction = Auction(route.Id, AuctionStatus.Open);
        var vehicle = Vehicle();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        ctx.Bids.Add(Bid(auction.Id, _carrierId, vehicle.Id, 100, BidStatus.Withdrawn));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetCostDeviationsAsync(_contractorId);
        result.Should().BeEmpty();

        var crossTenant = await Repo().GetCostDeviationsAsync(Guid.NewGuid());
        crossTenant.Should().BeEmpty();
    }

    // -------------------------------------------------------- GetSlaMilestonesAsync

    [Fact]
    public async Task GetSlaMilestonesAsync_returns_milestone_for_in_transit_trip()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, RouteStatus.InTransit);
        var auction = Auction(route.Id);
        var vehicle = Vehicle();
        var trip = Trip(route.Id, auction.Id, _carrierId, vehicle.Id, TripStatus.InTransit);
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.Trips.Add(trip);
        ctx.RouteSegments.AddRange(
            Segment(_contractorId, route.Id, SegmentStatus.Routed, "Curitiba, PR", "Joinville, SC",
                pickup: DateTimeOffset.UtcNow.AddHours(1), delivery: DateTimeOffset.UtcNow.AddHours(5)),
            Segment(_contractorId, route.Id, SegmentStatus.Routed, "Joinville, SC", "São Paulo, SP",
                pickup: DateTimeOffset.UtcNow.AddHours(2), delivery: DateTimeOffset.UtcNow.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetSlaMilestonesAsync(_contractorId);

        result.Should().ContainSingle();
        result[0].Itinerary.Should().Be("Curitiba \u2192 São Paulo");
        result[0].MilestoneType.Should().Be("COLETA");
        result[0].ReferenceCode.Should().StartWith("TRP-");
    }

    [Fact]
    public async Task GetSlaMilestonesAsync_uses_monitoring_next_stop_when_available()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, RouteStatus.InTransit);
        var auction = Auction(route.Id);
        var vehicle = Vehicle();
        var trip = Trip(route.Id, auction.Id, _carrierId, vehicle.Id, TripStatus.InTransit);
        var deadline = DateTimeOffset.UtcNow.AddHours(3);
        var eta = DateTimeOffset.UtcNow.AddHours(2);
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.Trips.Add(trip);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id, SegmentStatus.InTransit));
        ctx.TripMonitorings.Add(new TripMonitoring
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            NextStopDeadline = deadline,
            LastCalculatedEta = eta,
            LastPingAt = DateTimeOffset.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var result = await Repo().GetSlaMilestonesAsync(_contractorId);

        result.Should().ContainSingle();
        result[0].SlaDeadline.Should().BeCloseTo(deadline, TimeSpan.FromSeconds(1));
        result[0].LastCalculatedEta.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSlaMilestonesAsync_returns_empty_when_no_trips()
    {
        var result = await Repo().GetSlaMilestonesAsync(_contractorId);
        result.Should().BeEmpty();
    }

    // -------------------------------------------------------- GetCarrierKpisAsync

    [Fact]
    public async Task GetCarrierKpisAsync_counts_free_vehicles_active_bids_and_in_transit_trips()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId);
        var closedRoute = Route(_contractorId);
        var openAuction = Auction(route.Id, AuctionStatus.Open);
        var closedAuction = Auction(closedRoute.Id, AuctionStatus.Closed);
        var vehicle = Vehicle(status: OperationalStatus.EM_TRANSITO);
        var bidVehicle = Vehicle(plate: "BID1B11", status: OperationalStatus.LIVRE);
        ctx.ConsolidatedRoutes.AddRange(route, closedRoute);
        ctx.Auctions.AddRange(openAuction, closedAuction);
        ctx.Vehicles.AddRange(
            TestData.Vehicle(_carrierId, plate: "AAA1A11", status: OperationalStatus.LIVRE),
            TestData.Vehicle(_carrierId, plate: "BBB2B22", status: OperationalStatus.LIVRE),
            vehicle,
            bidVehicle);
        ctx.Bids.AddRange(
            Bid(openAuction.Id, _carrierId, bidVehicle.Id, 100, BidStatus.Pending),
            Bid(openAuction.Id, _carrierId, bidVehicle.Id, 90, BidStatus.Withdrawn),
            Bid(closedAuction.Id, _carrierId, bidVehicle.Id, 80, BidStatus.Pending));
        ctx.Trips.Add(Trip(route.Id, openAuction.Id, _carrierId, vehicle.Id, TripStatus.InTransit));
        await ctx.SaveChangesAsync();

        var kpis = await Repo().GetCarrierKpisAsync(_carrierId);

        kpis.AvailableVehicles.Should().Be(3);
        kpis.ActiveBids.Should().Be(1);
        kpis.InTransitTrips.Should().Be(1);
    }

    [Fact]
    public async Task GetCarrierKpisAsync_returns_zeros_for_unknown_carrier()
    {
        var kpis = await Repo().GetCarrierKpisAsync(Guid.NewGuid());
        kpis.AvailableVehicles.Should().Be(0);
        kpis.ActiveBids.Should().Be(0);
        kpis.InTransitTrips.Should().Be(0);
    }

    // ------------------------------------------ GetCarrierPerformanceInputsAsync

    [Fact]
    public async Task GetCarrierPerformanceInputsAsync_computes_fleet_and_bid_figures_for_month()
    {
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var ctx = _db.CreateContext();
        var route = Route(_contractorId);
        var auction = Auction(route.Id);
        var vehicle = Vehicle(plate: "AAA1A11", status: OperationalStatus.LIVRE);
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.AddRange(
            vehicle,
            TestData.Vehicle(_carrierId, plate: "BBB2B22", status: OperationalStatus.EM_TRANSITO),
            TestData.Vehicle(_carrierId, plate: "CCC3C33", status: OperationalStatus.MANUTENCAO));
        ctx.Bids.AddRange(
            Bid(auction.Id, _carrierId, vehicle.Id, 100, BidStatus.Pending, monthStart.AddDays(1)),
            Bid(auction.Id, _carrierId, vehicle.Id, 90, BidStatus.Winner, monthStart.AddDays(2)),
            Bid(auction.Id, _carrierId, vehicle.Id, 80, BidStatus.Pending, monthStart.AddDays(-5)));
        await ctx.SaveChangesAsync();

        var inputs = await Repo().GetCarrierPerformanceInputsAsync(_carrierId, monthStart);

        inputs.TotalVehicles.Should().Be(3);
        inputs.BusyVehicles.Should().Be(2);
        inputs.SubmittedBids.Should().Be(2);
        inputs.WonBids.Should().Be(1);
    }

    [Fact]
    public async Task GetCarrierPerformanceInputsAsync_returns_zeros_when_no_data()
    {
        var inputs = await Repo().GetCarrierPerformanceInputsAsync(Guid.NewGuid(), DateTimeOffset.UtcNow);
        inputs.TotalVehicles.Should().Be(0);
        inputs.BusyVehicles.Should().Be(0);
        inputs.SubmittedBids.Should().Be(0);
        inputs.WonBids.Should().Be(0);
    }

    // --------------------------------- GetInTransitOccupationsByCarrierAsync

    [Fact]
    public async Task GetInTransitOccupationsByCarrierAsync_returns_occupation_scoped_to_carrier()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, RouteStatus.InTransit, weight: 5000, volume: 20);
        var auction = Auction(route.Id);
        var vehicle = Vehicle();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.Trips.Add(Trip(route.Id, auction.Id, _carrierId, vehicle.Id, TripStatus.InTransit));
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id, SegmentStatus.InTransit));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetInTransitOccupationsByCarrierAsync(_carrierId);

        result.Should().ContainSingle();
        result[0].RouteWeightKg.Should().Be(5000);
        result[0].SegmentCount.Should().Be(1);

        var other = await Repo().GetInTransitOccupationsByCarrierAsync(Guid.NewGuid());
        other.Should().BeEmpty();
    }

    // ------------------------------------------------- GetActiveBidDisputesAsync

    [Fact]
    public async Task GetActiveBidDisputesAsync_returns_my_bid_and_leader_bid()
    {
        var otherCarrierId = Guid.NewGuid();
        var ctx = _db.CreateContext();
        ctx.Carriers.Add(TestData.Carrier(otherCarrierId, cnpj: "55555555000155"));
        var route = Route(_contractorId);
        var auction = Auction(route.Id, AuctionStatus.Open);
        var myVehicle = Vehicle();
        var otherVehicle = TestData.Vehicle(otherCarrierId, plate: "OTH1O11");
        otherVehicle.Id = Guid.NewGuid();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.AddRange(myVehicle, otherVehicle);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id, origin: "Curitiba, PR", destination: "São Paulo, SP"));
        ctx.Bids.AddRange(
            Bid(auction.Id, _carrierId, myVehicle.Id, 7000, BidStatus.Pending),
            Bid(auction.Id, otherCarrierId, otherVehicle.Id, 6000, BidStatus.Winning));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetActiveBidDisputesAsync(_carrierId);

        result.Should().ContainSingle();
        result[0].RouteId.Should().Be(route.Id);
        result[0].MyBidAmount.Should().Be(7000);
        result[0].LeaderBidAmount.Should().Be(6000);
        result[0].Itinerary.Should().Be("Curitiba \u2192 São Paulo");
    }

    [Fact]
    public async Task GetActiveBidDisputesAsync_returns_empty_when_no_active_bids()
    {
        var result = await Repo().GetActiveBidDisputesAsync(_carrierId);
        result.Should().BeEmpty();
    }

    // ------------------------------------------- GetCarrierSlaMilestonesAsync

    [Fact]
    public async Task GetCarrierSlaMilestonesAsync_returns_milestone_with_plate()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, RouteStatus.InTransit);
        var auction = Auction(route.Id);
        var vehicle = Vehicle(plate: "XYZ9Z99");
        var trip = Trip(route.Id, auction.Id, _carrierId, vehicle.Id, TripStatus.InTransit);
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.Trips.Add(trip);
        ctx.RouteSegments.Add(
            Segment(_contractorId, route.Id, SegmentStatus.Routed,
                pickup: DateTimeOffset.UtcNow.AddHours(1), delivery: DateTimeOffset.UtcNow.AddHours(6)));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetCarrierSlaMilestonesAsync(_carrierId);

        result.Should().ContainSingle();
        result[0].VehiclePlate.Should().Be("XYZ9Z99");
        result[0].MilestoneType.Should().Be("COLETA");
        result[0].ReferenceCode.Should().StartWith("TRP-");
    }

    [Fact]
    public async Task GetCarrierSlaMilestonesAsync_returns_entrega_when_no_routed_segment()
    {
        var ctx = _db.CreateContext();
        var route = Route(_contractorId, RouteStatus.InTransit);
        var auction = Auction(route.Id);
        var vehicle = Vehicle(plate: "ENT1R11");
        var trip = Trip(route.Id, auction.Id, _carrierId, vehicle.Id, TripStatus.InTransit);
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Vehicles.Add(vehicle);
        ctx.Trips.Add(trip);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id, SegmentStatus.Completed));
        await ctx.SaveChangesAsync();

        var result = await Repo().GetCarrierSlaMilestonesAsync(_carrierId);

        result.Should().ContainSingle();
        result[0].MilestoneType.Should().Be("ENTREGA");
    }

    [Fact]
    public async Task GetCarrierSlaMilestonesAsync_returns_empty_when_no_trips()
    {
        var result = await Repo().GetCarrierSlaMilestonesAsync(_carrierId);
        result.Should().BeEmpty();
    }
}
