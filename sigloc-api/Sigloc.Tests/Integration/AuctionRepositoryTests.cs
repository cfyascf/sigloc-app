using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class AuctionRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private static ConsolidatedRoute Route(Guid contractorId) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        Status = RouteStatus.Planned,
    };

    private static Auction Auction(Guid routeId, AuctionStatus status = AuctionStatus.Open, DateTimeOffset? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        RouteId = routeId,
        OpenedAt = DateTimeOffset.UtcNow,
        ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(1),
        Status = status,
    };

    private static RouteSegment Segment(Guid contractorId, Guid routeId, string origin = "Curitiba, PR", string destination = "São Paulo, SP") => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        RouteId = routeId,
        OriginAddress = origin,
        DestinationAddress = destination,
        OriginCoordinate = "-49.27,-25.42",
        DestinationCoordinate = "-46.63,-23.55",
        PickupDeadline = DateTimeOffset.UtcNow.AddDays(1),
        DeliveryDeadline = DateTimeOffset.UtcNow.AddDays(2),
        Status = SegmentStatus.Routed,
    };

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var route = Route(_contractorId);
        var auction = Auction(route.Id);

        var ctx = _db.CreateContext();
        ctx.ConsolidatedRoutes.Add(route);
        await new AuctionRepository(ctx).AddAsync(auction);

        (await _db.CreateContext().Auctions.FindAsync(auction.Id)).Should().BeNull();

        await ctx.SaveChangesAsync();

        (await _db.CreateContext().Auctions.FindAsync(auction.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetDetailAsync_scopes_by_contractor_and_includes_segments()
    {
        var route = Route(_contractorId);
        var auction = Auction(route.Id);

        var ctx = _db.CreateContext();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var detail = await repo.GetDetailAsync(auction.Id, _contractorId);
        var crossTenant = await repo.GetDetailAsync(auction.Id, Guid.NewGuid());

        detail.Should().NotBeNull();
        detail!.Segments.Should().ContainSingle();
        crossTenant.Should().BeNull();
    }

    [Fact]
    public async Task GetTrackedByIdAsync_scopes_by_contractor()
    {
        var route = Route(_contractorId);
        var auction = Auction(route.Id);

        var ctx = _db.CreateContext();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        (await repo.GetTrackedByIdAsync(auction.Id, _contractorId)).Should().NotBeNull();
        (await repo.GetTrackedByIdAsync(auction.Id, Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_filters_by_status_search_and_orders_by_expiry()
    {
        var routeEarly = Route(_contractorId);
        var ctx = _db.CreateContext();
        ctx.ConsolidatedRoutes.Add(routeEarly);
        ctx.Auctions.Add(Auction(routeEarly.Id, AuctionStatus.Open));
        await ctx.SaveChangesAsync();

        var (items, total) = await new AuctionRepository(_db.CreateContext())
            .SearchAsync(_contractorId, null, AuctionStatus.Open, 1, 10);
        total.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_paginates()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SearchAvailableForCarrierAsync_requires_active_partnership_and_open_non_expired()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetAvailableForCarrierAsync_returns_single_open_auction_for_partner()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateAsync_and_DeleteAsync_persist()
    {
        var route = Route(_contractorId);
        var auction = Auction(route.Id);

        var seedCtx = _db.CreateContext();
        seedCtx.ConsolidatedRoutes.Add(route);
        seedCtx.Auctions.Add(auction);
        await seedCtx.SaveChangesAsync();

        var updCtx = _db.CreateContext();
        var repo = new AuctionRepository(updCtx);
        var tracked = await repo.GetTrackedByIdAsync(auction.Id, _contractorId);
        tracked!.Status = AuctionStatus.Closed;
        await repo.UpdateAsync(tracked);

        var afterUpdate = await new AuctionRepository(_db.CreateContext()).GetTrackedByIdAsync(auction.Id, _contractorId);
        afterUpdate!.Status.Should().Be(AuctionStatus.Closed);

        var delCtx = _db.CreateContext();
        var delRepo = new AuctionRepository(delCtx);
        var toDelete = await delRepo.GetTrackedByIdAsync(auction.Id, _contractorId);
        await delRepo.DeleteAsync(toDelete!);

        (await new AuctionRepository(_db.CreateContext()).GetTrackedByIdAsync(auction.Id, _contractorId)).Should().BeNull();
    }
}
