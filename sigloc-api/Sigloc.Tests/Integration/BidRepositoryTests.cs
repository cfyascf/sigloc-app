using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class BidRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static Bid Bid(
        Guid auctionId,
        Guid carrierId,
        Guid vehicleId,
        decimal totalValue = 1000m,
        BidStatus status = BidStatus.Pending,
        DateTimeOffset? submittedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            CarrierId = carrierId,
            VehicleId = vehicleId,
            NetFreightValue = totalValue - 100m,
            TollValue = 100m,
            TotalValue = totalValue,
            SubmittedAt = submittedAt ?? DateTimeOffset.UtcNow,
            Status = status,
        };

    /// <summary>
    /// Seeds the FK parents a Bid requires (ConsolidatedRoute -> Auction, Carrier, Vehicle) so
    /// SQLite's foreign key enforcement is satisfied, and returns the created ids.
    /// </summary>
    private async Task<(Guid auctionId, Guid carrierId, Guid vehicleId)> SeedBidParentsAsync(
        SiglocDbContext ctx,
        Carrier? carrier = null,
        Vehicle? vehicle = null)
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
        carrier ??= TestData.Carrier(cnpj: UniqueCnpj());
        vehicle ??= TestData.Vehicle(carrier.Id, plate: UniquePlate());

        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        ctx.Carriers.Add(carrier);
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();

        return (auction.Id, carrier.Id, vehicle.Id);
    }

    /// <summary>Seeds a standalone route + auction and returns the auction id.</summary>
    private async Task<Guid> SeedExtraAuctionAsync()
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
        var ctx = _db.CreateContext();
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        await ctx.SaveChangesAsync();
        return auction.Id;
    }

    private static int _cnpjSeed;
    private static string UniqueCnpj() => (10000000000000L + System.Threading.Interlocked.Increment(ref _cnpjSeed)).ToString();
    private static int _plateSeed;
    private static string UniquePlate() => "P" + System.Threading.Interlocked.Increment(ref _plateSeed).ToString("D6");

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var seedCtx = _db.CreateContext();
        var (auctionId, carrierId, vehicleId) = await SeedBidParentsAsync(seedCtx);
        var bid = Bid(auctionId, carrierId, vehicleId);

        var ctx = _db.CreateContext();
        await new BidRepository(ctx).AddAsync(bid);

        (await _db.CreateContext().Bids.FindAsync(bid.Id)).Should().BeNull();

        await ctx.SaveChangesAsync();

        (await _db.CreateContext().Bids.FindAsync(bid.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCarrierAndAuctionAsync_returns_latest_non_withdrawn_bid()
    {
        var seedCtx = _db.CreateContext();
        var (auctionId, carrierId, vehicleId) = await SeedBidParentsAsync(seedCtx);
        var now = DateTimeOffset.UtcNow;

        var ctx = _db.CreateContext();
        ctx.Bids.AddRange(
            Bid(auctionId, carrierId, vehicleId, totalValue: 900m, submittedAt: now.AddMinutes(-20)),
            Bid(auctionId, carrierId, vehicleId, totalValue: 800m, submittedAt: now.AddMinutes(-5)),
            Bid(auctionId, carrierId, vehicleId, totalValue: 700m, status: BidStatus.Withdrawn, submittedAt: now));
        await ctx.SaveChangesAsync();

        var repo = new BidRepository(_db.CreateContext());
        var found = await repo.GetByCarrierAndAuctionAsync(carrierId, auctionId);
        found!.TotalValue.Should().Be(800m);
    }

    [Fact]
    public async Task GetMetricsForAuctionsAsync_empty_input_returns_empty()
    {
        var result = await new BidRepository(_db.CreateContext())
            .GetMetricsForAuctionsAsync(Array.Empty<Guid>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetricsForAuctionsAsync_returns_min_total_and_count_excluding_withdrawn()
    {
        var seedCtx = _db.CreateContext();
        var (auctionId, carrierId, vehicleId) = await SeedBidParentsAsync(seedCtx);

        var ctx = _db.CreateContext();
        ctx.Bids.AddRange(
            Bid(auctionId, carrierId, vehicleId, totalValue: 1200m),
            Bid(auctionId, carrierId, vehicleId, totalValue: 900m),
            Bid(auctionId, carrierId, vehicleId, totalValue: 500m, status: BidStatus.Withdrawn));
        await ctx.SaveChangesAsync();

        var result = await new BidRepository(_db.CreateContext())
            .GetMetricsForAuctionsAsync(new[] { auctionId });

        result.Should().ContainSingle();
        result[0].BestBid.Should().Be(900m);
        result[0].TotalBids.Should().Be(2);
    }

    [Fact]
    public async Task GetBestBidWithCarrierAsync_returns_lowest_total_with_carrier_name()
    {
        var seedCtx = _db.CreateContext();
        var carrierA = TestData.Carrier(cnpj: UniqueCnpj(), companyName: "Alpha Transportes");
        var (auctionId, _, vehicleId) = await SeedBidParentsAsync(seedCtx, carrier: carrierA);

        var ctx = _db.CreateContext();
        ctx.Bids.Add(Bid(auctionId, carrierA.Id, vehicleId, totalValue: 750m));
        await ctx.SaveChangesAsync();

        var best = await new BidRepository(_db.CreateContext()).GetBestBidWithCarrierAsync(auctionId);
        best!.CarrierName.Should().Be("Alpha Transportes");
    }

    [Fact]
    public async Task GetRankedBidsAsync_orders_by_total_then_rating_and_excludes_withdrawn()
    {
        var seedCtx = _db.CreateContext();
        var carrierLow = TestData.Carrier(cnpj: UniqueCnpj(), companyName: "Low", averageRating: 3.0);
        var carrierHigh = TestData.Carrier(cnpj: UniqueCnpj(), companyName: "High", averageRating: 4.9);
        var vehicle = TestData.Vehicle(carrierLow.Id, plate: UniquePlate());
        var (auctionId, _, _) = await SeedBidParentsAsync(seedCtx, carrier: carrierLow, vehicle: vehicle);

        var carrierCtx = _db.CreateContext();
        carrierCtx.Carriers.Add(carrierHigh);
        await carrierCtx.SaveChangesAsync();

        var ctx = _db.CreateContext();
        ctx.Bids.AddRange(
            // Same total -> higher rating ranks first.
            Bid(auctionId, carrierLow.Id, vehicle.Id, totalValue: 1000m),
            Bid(auctionId, carrierHigh.Id, vehicle.Id, totalValue: 1000m),
            Bid(auctionId, carrierHigh.Id, vehicle.Id, totalValue: 500m, status: BidStatus.Withdrawn));
        await ctx.SaveChangesAsync();

        var ranked = await new BidRepository(_db.CreateContext()).GetRankedBidsAsync(auctionId);

        ranked.Should().HaveCount(2);
        ranked[0].Carrier.CompanyName.Should().Be("High");
        ranked[1].Carrier.CompanyName.Should().Be("Low");
    }

    [Fact]
    public async Task GetTrackedByAuctionAsync_returns_all_bids_including_withdrawn()
    {
        var seedCtx = _db.CreateContext();
        var (auctionId, carrierId, vehicleId) = await SeedBidParentsAsync(seedCtx);

        var otherSeedCtx = _db.CreateContext();
        var (otherAuctionId, otherCarrierId, otherVehicleId) = await SeedBidParentsAsync(otherSeedCtx);

        var ctx = _db.CreateContext();
        ctx.Bids.AddRange(
            Bid(auctionId, carrierId, vehicleId),
            Bid(auctionId, carrierId, vehicleId, status: BidStatus.Withdrawn),
            Bid(otherAuctionId, otherCarrierId, otherVehicleId));
        await ctx.SaveChangesAsync();

        var tracked = await new BidRepository(_db.CreateContext()).GetTrackedByAuctionAsync(auctionId);

        tracked.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCarrierBidStatusesAsync_empty_input_returns_empty()
    {
        var result = await new BidRepository(_db.CreateContext())
            .GetCarrierBidStatusesAsync(Guid.NewGuid(), Array.Empty<Guid>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCarrierBidStatusesAsync_keeps_most_relevant_status_and_excludes_withdrawn()
    {
        var carrier = TestData.Carrier(cnpj: UniqueCnpj());
        var vehicle = TestData.Vehicle(carrier.Id, plate: UniquePlate());

        var ctx = _db.CreateContext();
        var (auctionWinner, carrierId, vehicleId) = await SeedBidParentsAsync(ctx, carrier: carrier, vehicle: vehicle);
        var auctionLosing = await SeedExtraAuctionAsync();
        var auctionOnlyWithdrawn = await SeedExtraAuctionAsync();

        var bidCtx = _db.CreateContext();
        bidCtx.Bids.AddRange(
            Bid(auctionWinner, carrierId, vehicleId, status: BidStatus.Losing),
            Bid(auctionWinner, carrierId, vehicleId, status: BidStatus.Winner),
            Bid(auctionLosing, carrierId, vehicleId, status: BidStatus.Losing),
            Bid(auctionOnlyWithdrawn, carrierId, vehicleId, status: BidStatus.Withdrawn));
        await bidCtx.SaveChangesAsync();

        var result = await new BidRepository(_db.CreateContext())
            .GetCarrierBidStatusesAsync(carrierId, new[] { auctionWinner, auctionLosing, auctionOnlyWithdrawn });

        result[auctionWinner].Should().Be(BidStatus.Winner);
        result[auctionLosing].Should().Be(BidStatus.Losing);
        result.Should().NotContainKey(auctionOnlyWithdrawn);
    }

    [Fact]
    public async Task GetCarrierBidStatusAsync_returns_max_status_or_null()
    {
        var seedCtx = _db.CreateContext();
        var (auctionId, carrierId, vehicleId) = await SeedBidParentsAsync(seedCtx);

        var ctx = _db.CreateContext();
        ctx.Bids.AddRange(
            Bid(auctionId, carrierId, vehicleId, status: BidStatus.Pending),
            Bid(auctionId, carrierId, vehicleId, status: BidStatus.Winning));
        await ctx.SaveChangesAsync();

        var repo = new BidRepository(_db.CreateContext());
        (await repo.GetCarrierBidStatusAsync(carrierId, auctionId)).Should().Be(BidStatus.Winning);
        (await repo.GetCarrierBidStatusAsync(Guid.NewGuid(), auctionId)).Should().BeNull();
    }
}
