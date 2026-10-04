using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class ProductRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _contractorId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_persists_and_GetByIdAsync_scopes_by_contractor()
    {
        var product = TestData.Product(_contractorId, sku: "SKU-A");
        await new ProductRepository(_db.CreateContext()).AddAsync(product);

        var repo = new ProductRepository(_db.CreateContext());
        var found = await repo.GetByIdAsync(product.Id, _contractorId);
        var crossTenant = await repo.GetByIdAsync(product.Id, Guid.NewGuid());

        found.Should().NotBeNull();
        found!.Sku.Should().Be("SKU-A");
        crossTenant.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_filters_by_term_category_and_orders_by_name()
    {
        var ctx = _db.CreateContext();
        ctx.Products.AddRange(
            TestData.Product(_contractorId, sku: "Z", name: "Zebra", category: ProductCategory.General),
            TestData.Product(_contractorId, sku: "A", name: "Abacaxi", category: ProductCategory.General),
            TestData.Product(_contractorId, sku: "B", name: "Bulk", category: ProductCategory.SolidBulk),
            TestData.Product(Guid.NewGuid(), sku: "X", name: "Alheio", category: ProductCategory.General));
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(_db.CreateContext());
        var (items, total) = await repo.SearchAsync(_contractorId, null, null, 1, 10);

        total.Should().Be(3);
        items.Select(i => i.Name).Should().ContainInOrder("Abacaxi", "Bulk", "Zebra");

        var (generalItems, generalTotal) = await repo.SearchAsync(_contractorId, null, ProductCategory.General, 1, 10);
        generalTotal.Should().Be(2);
        generalItems.Should().OnlyContain(i => i.Category == ProductCategory.General);

        var (searchItems, _) = await repo.SearchAsync(_contractorId, "aba", null, 1, 10);
        searchItems.Should().ContainSingle(i => i.Name == "Abacaxi");
    }

    [Fact]
    public async Task SearchAsync_paginates()
    {
        var ctx = _db.CreateContext();
        for (var i = 0; i < 5; i++)
            ctx.Products.Add(TestData.Product(_contractorId, sku: $"S{i}", name: $"Prod{i}"));
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(_db.CreateContext());
        var (page2, total) = await repo.SearchAsync(_contractorId, null, null, 2, 2);

        total.Should().Be(5);
        page2.Should().HaveCount(2);
        page2.Select(p => p.Name).Should().ContainInOrder("Prod2", "Prod3");
    }

    [Fact]
    public async Task SkuExistsAsync_respects_contractor_and_excludeId()
    {
        var product = TestData.Product(_contractorId, sku: "DUP");
        await new ProductRepository(_db.CreateContext()).AddAsync(product);

        var repo = new ProductRepository(_db.CreateContext());
        (await repo.SkuExistsAsync(_contractorId, "DUP")).Should().BeTrue();
        (await repo.SkuExistsAsync(_contractorId, "DUP", excludeId: product.Id)).Should().BeFalse();
        (await repo.SkuExistsAsync(Guid.NewGuid(), "DUP")).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_and_DeleteAsync_persist()
    {
        var product = TestData.Product(_contractorId, name: "Old");
        await new ProductRepository(_db.CreateContext()).AddAsync(product);

        var updating = new ProductRepository(_db.CreateContext());
        var toUpdate = await updating.GetByIdAsync(product.Id, _contractorId);
        toUpdate!.Name = "New";
        await updating.UpdateAsync(toUpdate);

        var afterUpdate = await new ProductRepository(_db.CreateContext()).GetByIdAsync(product.Id, _contractorId);
        afterUpdate!.Name.Should().Be("New");

        var deleting = new ProductRepository(_db.CreateContext());
        await deleting.DeleteAsync(afterUpdate);

        var afterDelete = await new ProductRepository(_db.CreateContext()).GetByIdAsync(product.Id, _contractorId);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_sets_audit_timestamps_via_SaveChanges_override()
    {
        var product = TestData.Product(_contractorId);
        await new ProductRepository(_db.CreateContext()).AddAsync(product);

        var saved = await new ProductRepository(_db.CreateContext()).GetByIdAsync(product.Id, _contractorId);

        saved!.CreatedAt.Should().NotBe(default);
        saved.UpdatedAt.Should().NotBe(default);
    }
}
