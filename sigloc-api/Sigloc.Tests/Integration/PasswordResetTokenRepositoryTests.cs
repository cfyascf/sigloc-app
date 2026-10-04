using Sigloc.Domain.Entities;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class PasswordResetTokenRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private static PasswordResetToken Token(
        Guid userId,
        string hash,
        DateTimeOffset expiresAt,
        DateTimeOffset? usedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expiresAt,
            UsedAt = usedAt,
        };

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var now = DateTimeOffset.UtcNow;
        var token = Token(_userId, "hash-add", now.AddHours(1));

        var ctx = _db.CreateContext();
        await new PasswordResetTokenRepository(ctx).AddAsync(token);

        // Verified via a direct lookup rather than GetValidByHashAsync, whose DateTimeOffset
        // comparison cannot be translated by SQLite.
        (await _db.CreateContext().PasswordResetTokens.FindAsync(token.Id)).Should().BeNull();

        await ctx.SaveChangesAsync();

        (await _db.CreateContext().PasswordResetTokens.FindAsync(token.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetValidByHashAsync_excludes_used_and_expired_tokens()
    {
        var now = DateTimeOffset.UtcNow;

        var ctx = _db.CreateContext();
        ctx.PasswordResetTokens.AddRange(
            Token(_userId, "valid", now.AddHours(1)),
            Token(_userId, "used", now.AddHours(1), usedAt: now.AddMinutes(-5)),
            Token(_userId, "expired", now.AddMinutes(-1)));
        await ctx.SaveChangesAsync();

        var repo = new PasswordResetTokenRepository(_db.CreateContext());
        (await repo.GetValidByHashAsync("valid", now)).Should().NotBeNull();
        (await repo.GetValidByHashAsync("used", now)).Should().BeNull();
        (await repo.GetValidByHashAsync("expired", now)).Should().BeNull();
    }

    [Fact]
    public async Task InvalidateActiveForUserAsync_marks_active_tokens_used_but_requires_caller_save()
    {
        var now = DateTimeOffset.UtcNow;

        var seedCtx = _db.CreateContext();
        seedCtx.PasswordResetTokens.AddRange(
            Token(_userId, "active-1", now.AddHours(1)),
            Token(_userId, "active-2", now.AddHours(2)),
            Token(_userId, "already-used", now.AddHours(1), usedAt: now.AddMinutes(-10)),
            Token(Guid.NewGuid(), "other-user", now.AddHours(1)));
        await seedCtx.SaveChangesAsync();

        var actCtx = _db.CreateContext();
        await new PasswordResetTokenRepository(actCtx).InvalidateActiveForUserAsync(_userId, now);

        // Repository does not SaveChanges; a fresh context still sees the tokens as valid.
        var repoBefore = new PasswordResetTokenRepository(_db.CreateContext());
        (await repoBefore.GetValidByHashAsync("active-1", now)).Should().NotBeNull();

        await actCtx.SaveChangesAsync();

        var repoAfter = new PasswordResetTokenRepository(_db.CreateContext());
        (await repoAfter.GetValidByHashAsync("active-1", now)).Should().BeNull();
        (await repoAfter.GetValidByHashAsync("active-2", now)).Should().BeNull();
        // Other user's token is untouched.
        (await repoAfter.GetValidByHashAsync("other-user", now)).Should().NotBeNull();
    }
}
