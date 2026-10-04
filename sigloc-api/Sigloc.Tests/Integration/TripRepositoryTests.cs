using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class TripRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static int _cnpjSeed;
    private static string UniqueCnpj() => (20000000000000L + System.Threading.Interlocked.Increment(ref _cnpjSeed)).ToString();
    private static int _plateSeed;
    private static string UniquePlate() => "T" + System.Threading.Interlocked.Increment(ref _plateSeed).ToString("D6");

    /// <summary>
    /// Seeds the FK parents a Trip requires (ConsolidatedRoute, Auction, Carrier, Vehicle) so
    /// SQLite's foreign key enforcement is satisfied, and returns a ready-to-persist Trip.
    /// </summary>
    private async Task<Trip> SeedTripAsync(SiglocDbContext ctx, TripStatus status = TripStatus.AwaitingPickup)
    {
        var route = new ConsolidatedRoute { Id = Guid.NewGuid(), ContractorId = Guid.NewGuid(), Status = RouteStatus.Planned };
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            RouteId = route.Id,
            OpenedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = AuctionStatus.Open,
        };
        var carrier = TestData.Carrier(cnpj: UniqueCnpj());
        var vehicle = TestData.Vehicle(carrier.Id, plate: UniquePlate());

        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Carriers.Add(carrier);
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();

        return new Trip
        {
            Id = Guid.NewGuid(),
            RouteId = route.Id,
            AuctionId = auction.Id,
            CarrierId = carrier.Id,
            VehicleId = vehicle.Id,
            BidId = Guid.NewGuid(),
            AgreedValue = 1000m,
            Status = status,
        };
    }

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var seedCtx = _db.CreateContext();
        var trip = await SeedTripAsync(seedCtx);

        var ctx = _db.CreateContext();
        await new TripRepository(ctx).AddAsync(trip);

        (await _db.CreateContext().Trips.FindAsync(trip.Id)).Should().BeNull();

        await ctx.SaveChangesAsync();

        (await _db.CreateContext().Trips.FindAsync(trip.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveWindowsForVehicleAsync_aggregates_min_pickup_and_max_delivery()
    {
        var seedCtx = _db.CreateContext();
        var trip = await SeedTripAsync(seedCtx, TripStatus.InTransit);
        var basis = DateTimeOffset.UtcNow;

        var ctx = _db.CreateContext();
        ctx.Trips.Add(trip);
        // The repository inner-joins trips to route segments on RouteId, so the window
        // only materializes when the route has at least one segment.
        ctx.RouteSegments.Add(RouteSegmentOnRoute(trip.RouteId, basis.AddHours(2), basis.AddHours(10)));
        ctx.RouteSegments.Add(RouteSegmentOnRoute(trip.RouteId, basis.AddHours(5), basis.AddHours(20)));
        await ctx.SaveChangesAsync();

        var windows = await new TripRepository(_db.CreateContext()).GetActiveWindowsForVehicleAsync(trip.VehicleId);

        windows.Should().ContainSingle();
        windows[0].Start.Should().BeCloseTo(basis.AddHours(2), TimeSpan.FromSeconds(1));
        windows[0].End.Should().BeCloseTo(basis.AddHours(20), TimeSpan.FromSeconds(1));
    }

    private static RouteSegment RouteSegmentOnRoute(Guid? routeId, DateTimeOffset pickup, DateTimeOffset delivery) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = Guid.NewGuid(),
        RouteId = routeId,
        OriginAddress = "Curitiba, PR",
        DestinationAddress = "São Paulo, SP",
        OriginCoordinate = "-49.27,-25.42",
        DestinationCoordinate = "-46.63,-23.55",
        PickupDeadline = pickup,
        DeliveryDeadline = delivery,
    };

    [Theory]
    [InlineData(TripStatus.Delivered)]
    [InlineData(TripStatus.Cancelled)]
    public async Task GetActiveWindowsForVehicleAsync_excludes_delivered_and_cancelled_trips(TripStatus status)
    {
        var seedCtx = _db.CreateContext();
        var trip = await SeedTripAsync(seedCtx, status);
        var basis = DateTimeOffset.UtcNow;

        var ctx = _db.CreateContext();
        ctx.Trips.Add(trip);
        ctx.RouteSegments.Add(RouteSegmentOnRoute(trip.RouteId, basis.AddHours(2), basis.AddHours(10)));
        await ctx.SaveChangesAsync();

        var windows = await new TripRepository(_db.CreateContext()).GetActiveWindowsForVehicleAsync(trip.VehicleId);

        windows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveWindowsForVehicleAsync_returns_empty_for_unknown_vehicle()
    {
        var windows = await new TripRepository(_db.CreateContext()).GetActiveWindowsForVehicleAsync(Guid.NewGuid());

        windows.Should().BeEmpty();
    }
}
