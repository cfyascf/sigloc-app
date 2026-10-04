using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class UnitOfWorkTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SaveChangesAsync_persists_tracked_changes()
    {
        var product = TestData.Product();

        var ctx = _db.CreateContext();
        ctx.Products.Add(product);
        var uow = new UnitOfWork(ctx);
        var affected = await uow.SaveChangesAsync();

        affected.Should().BeGreaterThan(0);
        var saved = await _db.CreateContext().Products.FindAsync(product.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_commits_on_success()
    {
        var product = TestData.Product();

        var ctx = _db.CreateContext();
        var uow = new UnitOfWork(ctx);

        var result = await uow.ExecuteInTransactionAsync(async ct =>
        {
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync(ct);
            return product.Id;
        });

        result.Should().Be(product.Id);
        var saved = await _db.CreateContext().Products.FindAsync(product.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_rolls_back_when_delegate_throws()
    {
        var product = TestData.Product();

        var ctx = _db.CreateContext();
        var uow = new UnitOfWork(ctx);

        var act = async () => await uow.ExecuteInTransactionAsync<int>(async ct =>
        {
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync(ct);
            throw new InvalidOperationException("boom");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        // A fresh context must not see the row that was inserted inside the rolled-back transaction.
        var saved = await _db.CreateContext().Products.FindAsync(product.Id);
        saved.Should().BeNull();
    }
}
