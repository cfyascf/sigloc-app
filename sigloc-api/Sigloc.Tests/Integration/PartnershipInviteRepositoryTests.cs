using Sigloc.Domain.Entities;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class PartnershipInviteRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private static PartnershipInvite Invite(
        Guid contractorId,
        string token,
        bool isUsed = false,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? createdAt = null) => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId,
            Token = token,
            IsUsed = isUsed,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var invite = Invite(_contractorId, "token-add");

        var ctx = _db.CreateContext();
        await new PartnershipInviteRepository(ctx).AddAsync(invite);

        (await new PartnershipInviteRepository(_db.CreateContext()).GetByTokenAsync("token-add")).Should().BeNull();

        await ctx.SaveChangesAsync();

        (await new PartnershipInviteRepository(_db.CreateContext()).GetByTokenAsync("token-add")).Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_matches_exact_token()
    {
        var ctx = _db.CreateContext();
        ctx.PartnershipInvites.Add(Invite(_contractorId, "exact-token"));
        await ctx.SaveChangesAsync();

        var repo = new PartnershipInviteRepository(_db.CreateContext());
        (await repo.GetByTokenAsync("exact-token")).Should().NotBeNull();
        (await repo.GetByTokenAsync("missing-token")).Should().BeNull();
    }

    // The DbContext SaveChanges override stamps CreatedAt=utcNow on insert, overwriting
    // any seeded value, so distinct timestamps are applied with a follow-up update (the
    // override leaves CreatedAt untouched on Modified entities).
    private async Task SeedWithCreatedAtAsync(params (PartnershipInvite Invite, DateTimeOffset CreatedAt)[] rows)
    {
        var ctx = _db.CreateContext();
        ctx.PartnershipInvites.AddRange(rows.Select(r => r.Invite));
        await ctx.SaveChangesAsync();
        foreach (var (invite, createdAt) in rows)
        {
            invite.CreatedAt = createdAt;
        }
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLatestActiveByContractorAsync_returns_most_recent_non_used_non_expired()
    {
        var now = DateTimeOffset.UtcNow;

        await SeedWithCreatedAtAsync(
            (Invite(_contractorId, "old-active"), now.AddHours(-3)),
            (Invite(_contractorId, "new-active", expiresAt: now.AddDays(1)), now.AddHours(-1)),
            (Invite(_contractorId, "used", isUsed: true), now),
            (Invite(_contractorId, "expired", expiresAt: now.AddMinutes(-1)), now),
            (Invite(Guid.NewGuid(), "other-contractor"), now));

        var latest = await new PartnershipInviteRepository(_db.CreateContext())
            .GetLatestActiveByContractorAsync(_contractorId);

        latest.Should().NotBeNull();
        latest!.Token.Should().Be("new-active");
    }

    [Fact]
    public async Task GetLatestActiveByContractorAsync_returns_null_when_only_used_or_expired()
    {
        var now = DateTimeOffset.UtcNow;

        var ctx = _db.CreateContext();
        ctx.PartnershipInvites.AddRange(
            Invite(_contractorId, "used-only", isUsed: true, createdAt: now),
            Invite(_contractorId, "expired-only", createdAt: now, expiresAt: now.AddMinutes(-1)));
        await ctx.SaveChangesAsync();

        var latest = await new PartnershipInviteRepository(_db.CreateContext())
            .GetLatestActiveByContractorAsync(_contractorId);

        latest.Should().BeNull();
    }
}
