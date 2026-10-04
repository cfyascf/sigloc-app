using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var user = TestData.User(email: "deferred@example.com");

        var ctx = _db.CreateContext();
        var repo = new UserRepository(ctx);
        await repo.AddAsync(user);

        // Repository does not call SaveChanges; a fresh context sees nothing yet.
        var beforeSave = await new UserRepository(_db.CreateContext()).GetByIdAsync(user.Id);
        beforeSave.Should().BeNull();

        await ctx.SaveChangesAsync();

        var afterSave = await new UserRepository(_db.CreateContext()).GetByIdAsync(user.Id);
        afterSave.Should().NotBeNull();
        afterSave!.Email.Should().Be("deferred@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_returns_matching_user()
    {
        var ctx = _db.CreateContext();
        await new UserRepository(ctx).AddAsync(TestData.User(email: "find@example.com"));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(_db.CreateContext());
        (await repo.GetByEmailAsync("find@example.com")).Should().NotBeNull();
        (await repo.GetByEmailAsync("missing@example.com")).Should().BeNull();
    }

    [Fact]
    public async Task GetByGoogleIdAsync_returns_matching_user()
    {
        var user = TestData.User(email: "google@example.com", passwordHash: null, provider: AuthProvider.Google);
        user.GoogleId = "google-sub-123";

        var ctx = _db.CreateContext();
        await new UserRepository(ctx).AddAsync(user);
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(_db.CreateContext());
        (await repo.GetByGoogleIdAsync("google-sub-123")).Should().NotBeNull();
        (await repo.GetByGoogleIdAsync("other-sub")).Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_reflects_persistence()
    {
        var ctx = _db.CreateContext();
        await new UserRepository(ctx).AddAsync(TestData.User(email: "exists@example.com"));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(_db.CreateContext());
        (await repo.EmailExistsAsync("exists@example.com")).Should().BeTrue();
        (await repo.EmailExistsAsync("nobody@example.com")).Should().BeFalse();
    }
}
