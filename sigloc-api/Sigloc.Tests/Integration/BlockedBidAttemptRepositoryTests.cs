using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class BlockedBidAttemptRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();
    private readonly Guid _carrierId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private BlockedBidAttempt Attempt(
        Guid auctionId,
        BlockedReason reason = BlockedReason.Weight,
        Guid? carrierId = null) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = _contractorId,
        AuctionId = auctionId,
        CarrierId = carrierId ?? _carrierId,
        Reason = reason,
        AttemptedAt = DateTimeOffset.UtcNow,
    };

    private async Task<Guid> SeedAuctionAsync()
    {
        var ctx = _db.CreateContext();
        ctx.Contractors.Add(TestData.Contractor(_contractorId));
        var route = new ConsolidatedRoute { Id = Guid.NewGuid(), ContractorId = _contractorId, Status = RouteStatus.Planned };
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            RouteId = route.Id,
            OpenedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = AuctionStatus.Open,
        };
        ctx.ConsolidatedRoutes.Add(route);
        ctx.Auctions.Add(auction);
        await ctx.SaveChangesAsync();
        return auction.Id;
    }

    [Fact]
    public async Task AddAsync_persists_immediately()
    {
        var auctionId = await SeedAuctionAsync();
        var attempt = Attempt(auctionId, BlockedReason.Sla);

        await new BlockedBidAttemptRepository(_db.CreateContext()).AddAsync(attempt);

        var saved = await _db.CreateContext().BlockedBidAttempts.FindAsync(attempt.Id);
        saved.Should().NotBeNull();
        saved!.Reason.Should().Be(BlockedReason.Sla);
        saved.ContractorId.Should().Be(_contractorId);
        saved.CarrierId.Should().Be(_carrierId);
    }

    [Fact]
    public async Task AddAsync_stamps_created_at_audit_timestamp()
    {
        var auctionId = await SeedAuctionAsync();
        var attempt = Attempt(auctionId);

        await new BlockedBidAttemptRepository(_db.CreateContext()).AddAsync(attempt);

        var saved = await _db.CreateContext().BlockedBidAttempts.FindAsync(attempt.Id);
        saved!.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task AddAsync_allows_null_carrier()
    {
        var auctionId = await SeedAuctionAsync();
        var attempt = Attempt(auctionId);
        attempt.CarrierId = null;

        await new BlockedBidAttemptRepository(_db.CreateContext()).AddAsync(attempt);

        var saved = await _db.CreateContext().BlockedBidAttempts.FindAsync(attempt.Id);
        saved!.CarrierId.Should().BeNull();
    }
}
