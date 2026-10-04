using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

/// <summary>
/// Covers the carrier-facing AuctionRepository methods (networking-restricted search and
/// lookup) that the base AuctionRepositoryTests leaves as empty placeholders.
/// </summary>
public class AuctionRepositoryCarrierTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();
    private readonly Guid _carrierId = Guid.NewGuid();

    public AuctionRepositoryCarrierTests()
    {
        // PartnerConnection has FKs to both Carrier and Contractor (SQLite enforces them).
        var ctx = _db.CreateContext();
        ctx.Carriers.Add(TestData.Carrier(_carrierId));
        ctx.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private static ConsolidatedRoute Route(Guid contractorId) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        Status = RouteStatus.InAuction,
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

    private PartnerConnection Partnership(Guid contractorId, PartnershipStatus status = PartnershipStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        CarrierId = _carrierId,
        Status = status,
        InitiatedBy = PartnershipInitiator.Contractor,
    };

    [Fact]
    public async Task GetAvailableForCarrierAsync_returns_auction_for_active_partner()
    {
        var route = Route(_contractorId);
        var auction = Auction(route.Id);
        var contractor = TestData.Contractor(_contractorId);

        var ctx = _db.CreateContext();
        ctx.Contractors.Add(contractor);
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        ctx.PartnerConnections.Add(Partnership(_contractorId));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var result = await repo.GetAvailableForCarrierAsync(auction.Id, _carrierId, DateTimeOffset.UtcNow);

        result.Should().NotBeNull();
        result!.Segments.Should().ContainSingle();
        result.ContractorName.Should().Be(contractor.CompanyName);
    }

    [Fact]
    public async Task GetAvailableForCarrierAsync_null_without_active_partnership()
    {
        var route = Route(_contractorId);
        var auction = Auction(route.Id);

        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        ctx.PartnerConnections.Add(Partnership(_contractorId, PartnershipStatus.Pending));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var result = await repo.GetAvailableForCarrierAsync(auction.Id, _carrierId, DateTimeOffset.UtcNow);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailableForCarrierAsync_null_for_expired_or_closed_auction()
    {
        var route = Route(_contractorId);
        var expired = Auction(route.Id, AuctionStatus.Open, DateTimeOffset.UtcNow.AddMinutes(-1));
        var closed = Auction(route.Id, AuctionStatus.Closed);

        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.AddRange(expired, closed);
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        ctx.PartnerConnections.Add(Partnership(_contractorId));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var now = DateTimeOffset.UtcNow;

        (await repo.GetAvailableForCarrierAsync(expired.Id, _carrierId, now)).Should().BeNull();
        (await repo.GetAvailableForCarrierAsync(closed.Id, _carrierId, now)).Should().BeNull();
    }

    [Fact]
    public async Task SearchAvailableForCarrierAsync_returns_only_partnered_open_non_expired()
    {
        var partnerRoute = Route(_contractorId);
        var otherContractorId = Guid.NewGuid();
        var otherRoute = Route(otherContractorId);

        var ctx = _db.CreateContext();
        ctx.Contractors.AddRange(
            TestData.Contractor(_contractorId, cnpj: "11111111000111", companyName: "Partner Co"),
            TestData.Contractor(otherContractorId, cnpj: "22222222000122", companyName: "Stranger Co"));
        ctx.ConsolidatedRoutes.AddRange(partnerRoute, otherRoute);
        ctx.Auctions.AddRange(
            Auction(partnerRoute.Id, AuctionStatus.Open),
            Auction(otherRoute.Id, AuctionStatus.Open));
        ctx.RouteSegments.AddRange(
            Segment(_contractorId, partnerRoute.Id),
            Segment(otherContractorId, otherRoute.Id));
        ctx.PartnerConnections.Add(Partnership(_contractorId));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var (items, total) = await repo.SearchAvailableForCarrierAsync(_carrierId, null, DateTimeOffset.UtcNow, 1, 10);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].Route.Id.Should().Be(partnerRoute.Id);
        items[0].Segments.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchAvailableForCarrierAsync_filters_by_search_term()
    {
        var routeA = Route(_contractorId);
        var routeB = Route(_contractorId);

        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        ctx.ConsolidatedRoutes.AddRange(routeA, routeB);
        ctx.Auctions.AddRange(Auction(routeA.Id), Auction(routeB.Id));
        ctx.RouteSegments.AddRange(
            Segment(_contractorId, routeA.Id, "Curitiba, PR", "Florianópolis, SC"),
            Segment(_contractorId, routeB.Id, "Manaus, AM", "Belém, PA"));
        ctx.PartnerConnections.Add(Partnership(_contractorId));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var (items, total) = await repo.SearchAvailableForCarrierAsync(_carrierId, "manaus", DateTimeOffset.UtcNow, 1, 10);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].Route.Id.Should().Be(routeB.Id);
    }

    [Fact]
    public async Task SearchAvailableForCarrierAsync_paginates_ordered_by_expiry()
    {
        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        ctx.PartnerConnections.Add(Partnership(_contractorId));

        for (var i = 0; i < 3; i++)
        {
            var route = Route(_contractorId);
            ctx.ConsolidatedRoutes.Add(route);
            ctx.Auctions.Add(Auction(route.Id, AuctionStatus.Open, DateTimeOffset.UtcNow.AddDays(i + 1)));
            ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        }
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var (page1, total) = await repo.SearchAvailableForCarrierAsync(_carrierId, null, DateTimeOffset.UtcNow, 1, 2);
        var (page2, _) = await repo.SearchAvailableForCarrierAsync(_carrierId, null, DateTimeOffset.UtcNow, 2, 2);

        total.Should().Be(3);
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAvailableForCarrierAsync_returns_empty_when_no_partnership()
    {
        var route = Route(_contractorId);
        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(Auction(route.Id));
        ctx.RouteSegments.Add(Segment(_contractorId, route.Id));
        await ctx.SaveChangesAsync();

        var repo = new AuctionRepository(_db.CreateContext());
        var (items, total) = await repo.SearchAvailableForCarrierAsync(_carrierId, null, DateTimeOffset.UtcNow, 1, 10);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }
}
