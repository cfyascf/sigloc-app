using System.Reflection;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Tests;

public class AuctionMonitoringTests
{
    [Fact]
    public async Task Award_stages_frozen_operational_itinerary_inside_existing_transaction()
    {
        var companyId = Guid.NewGuid();
        var route = new ConsolidatedRoute { Id = Guid.NewGuid(), ContractorId = companyId, TotalDistanceKm = 123 };
        var auction = new Auction { Id = Guid.NewGuid(), RouteId = route.Id, Status = AuctionStatus.Open };
        var bid = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = Guid.NewGuid(), VehicleId = Guid.NewGuid() };
        var first = TripMonitoringServiceTests.Segment(); first.RouteSequence = 2;
        var second = TripMonitoringServiceTests.Segment(); second.RouteSequence = 1;
        var unit = new UnitOfWork(); Trip? staged = null;
        var auctions = Stub<IAuctionRepository>.New((name, _) => name switch
        {
            "GetTrackedByIdAsync" => Task.FromResult<Auction?>(auction),
            "GetDetailAsync" => Task.FromResult<AuctionWithRoute?>(new(auction, route, new[] { first, second })),
            _ => throw new NotSupportedException(name)
        });
        var bids = Stub<IBidRepository>.New((name, _) => name == "GetTrackedByAuctionAsync" ? Task.FromResult<IReadOnlyList<Bid>>(new[] { bid }) : throw new NotSupportedException(name));
        var routes = Stub<IConsolidatedRouteRepository>.New((_, _) => Task.FromResult<ConsolidatedRoute?>(route));
        var trips = Stub<ITripRepository>.New((_, args) => { Assert.True(unit.InTransaction); staged = (Trip)args![0]!; Assert.NotEmpty(staged.Stops); return Task.CompletedTask; });
        var service = new AuctionService(null!, routes, auctions, bids, trips, null!, null!, null!, null!, unit);
        await service.AwardAsync(companyId, auction.Id, new AwardAuctionRequestDto(bid.Id));
        Assert.NotNull(staged); Assert.Equal(second.Id, staged.Stops[0].Actions[0].SegmentId);
        Assert.Equal(TripStatus.AwaitingPickup, staged.Status); Assert.Equal(RouteStatus.AwaitingPickup, route.Status);
        Assert.Equal(123, route.TotalDistanceKm); Assert.Equal(1, unit.Commits);
    }

    [Fact]
    public async Task Creation_persists_input_sequence_and_includes_connecting_leg_distance()
    {
        var first = TripMonitoringServiceTests.Segment(); first.Status = SegmentStatus.Available; first.DistanceKm = 10;
        var second = TripMonitoringServiceTests.Segment(); second.Status = SegmentStatus.Available; second.DistanceKm = 20;
        var unit = new UnitOfWork(); ConsolidatedRoute? route = null;
        var segments = Stub<IRouteSegmentRepository>.New((_, _) => Task.FromResult<IReadOnlyList<RouteSegment>>(new[] { second, first }));
        var routes = Stub<IConsolidatedRouteRepository>.New((_, args) => { Assert.True(unit.InTransaction); route = (ConsolidatedRoute)args![0]!; return Task.CompletedTask; });
        var auctions = Stub<IAuctionRepository>.New((_, _) => { Assert.True(unit.InTransaction); return Task.CompletedTask; });
        var geography = Stub<IRouteGeocodingService>.New((name, args) =>
        {
            Assert.Equal("ComputeLegAsync", name); Assert.Equal(first.DestinationCoordinate, args![0]);
            Assert.Equal(second.OriginCoordinate, args[1]); return Task.FromResult(new RouteLeg(5, 0.5));
        });
        var notifier = Stub<IAuctionNotifier>.New((_, _) => Task.CompletedTask);
        var service = new AuctionService(segments, routes, auctions, null!, null!, null!, null!, geography, notifier, unit);
        await service.CreateAsync(Guid.NewGuid(), new CreateAuctionRequestDto(new[] { first.Id, second.Id }, DateTimeOffset.UtcNow.AddHours(1), false));
        Assert.Equal(1, first.RouteSequence); Assert.Equal(2, second.RouteSequence);
        Assert.NotNull(route); Assert.Equal(35, route.TotalDistanceKm); Assert.Equal(1, unit.Commits);
    }

    public class Stub<T> : DispatchProxy where T : class
    {
        public Func<string, object?[]?, object?> Handler { get; set; } = null!;
        public static T New(Func<string, object?[]?, object?> handler)
        {
            var proxy = Create<T, Stub<T>>(); ((Stub<T>)(object)proxy).Handler = handler; return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!.Name, args);
    }
    private sealed class UnitOfWork : IUnitOfWork
    {
        public bool InTransaction { get; private set; }
        public int Commits { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Assert.True(InTransaction); return Task.FromResult(1); }
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            InTransaction = true;
            try { var result = await operation(cancellationToken); Commits++; return result; }
            finally { InTransaction = false; }
        }
    }
}
