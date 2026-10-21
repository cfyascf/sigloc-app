using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class ContractorRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var contractor = TestData.Contractor(cnpj: "11222333000181");

        var ctx = _db.CreateContext();
        await new ContractorRepository(ctx).AddAsync(contractor);

        var beforeSave = await new ContractorRepository(_db.CreateContext()).GetByIdAsync(contractor.Id);
        beforeSave.Should().BeNull();

        await ctx.SaveChangesAsync();

        var afterSave = await new ContractorRepository(_db.CreateContext()).GetByIdAsync(contractor.Id);
        afterSave.Should().NotBeNull();
        afterSave!.Cnpj.Should().Be("11222333000181");
    }

    [Fact]
    public async Task CnpjExistsAsync_reflects_persistence()
    {
        var ctx = _db.CreateContext();
        await new ContractorRepository(ctx).AddAsync(TestData.Contractor(cnpj: "99887766000155"));
        await ctx.SaveChangesAsync();

        var repo = new ContractorRepository(_db.CreateContext());
        (await repo.CnpjExistsAsync("99887766000155")).Should().BeTrue();
        (await repo.CnpjExistsAsync("00000000000000")).Should().BeFalse();
    }

    [Fact]
    public async Task GetTrackedByIdAsync_returns_tracked_entity()
    {
        var contractor = TestData.Contractor(cnpj: "22333444000122", companyName: "Rastreada Ltda");
        var seed = _db.CreateContext();
        await new ContractorRepository(seed).AddAsync(contractor);
        await seed.SaveChangesAsync();

        var ctx = _db.CreateContext();
        var tracked = await new ContractorRepository(ctx).GetTrackedByIdAsync(contractor.Id);

        tracked.Should().NotBeNull();
        ctx.Entry(tracked!).State.Should().Be(Microsoft.EntityFrameworkCore.EntityState.Unchanged);
    }

    [Fact]
    public async Task UpdateAsync_persists_editable_fields()
    {
        var contractor = TestData.Contractor(cnpj: "33444555000133", companyName: "Nome Antigo");
        var seed = _db.CreateContext();
        await new ContractorRepository(seed).AddAsync(contractor);
        await seed.SaveChangesAsync();

        var ctx = _db.CreateContext();
        var repo = new ContractorRepository(ctx);
        var tracked = await repo.GetTrackedByIdAsync(contractor.Id);
        tracked!.CompanyName = "Nome Novo";
        tracked.TradeName = "Fantasia Nova";
        await repo.UpdateAsync(tracked);

        var reloaded = await new ContractorRepository(_db.CreateContext()).GetByIdAsync(contractor.Id);
        reloaded!.CompanyName.Should().Be("Nome Novo");
        reloaded.TradeName.Should().Be("Fantasia Nova");
    }
}

public class CarrierRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var carrier = TestData.Carrier(cnpj: "99888777000166");

        var ctx = _db.CreateContext();
        await new CarrierRepository(ctx).AddAsync(carrier);

        var beforeSave = await new CarrierRepository(_db.CreateContext()).GetByCnpjAsync("99888777000166");
        beforeSave.Should().BeNull();

        await ctx.SaveChangesAsync();

        var afterSave = await new CarrierRepository(_db.CreateContext()).GetByCnpjAsync("99888777000166");
        afterSave.Should().NotBeNull();
        afterSave!.CompanyName.Should().Be("Transportadora Ltda");
    }

    [Fact]
    public async Task CnpjExistsAsync_reflects_persistence()
    {
        var ctx = _db.CreateContext();
        await new CarrierRepository(ctx).AddAsync(TestData.Carrier(cnpj: "12121212000112"));
        await ctx.SaveChangesAsync();

        var repo = new CarrierRepository(_db.CreateContext());
        (await repo.CnpjExistsAsync("12121212000112")).Should().BeTrue();
        (await repo.CnpjExistsAsync("00000000000000")).Should().BeFalse();
    }

    [Fact]
    public async Task GetTrackedByIdAsync_returns_tracked_entity()
    {
        var carrier = TestData.Carrier(cnpj: "44555666000144");
        var seed = _db.CreateContext();
        await new CarrierRepository(seed).AddAsync(carrier);
        await seed.SaveChangesAsync();

        var ctx = _db.CreateContext();
        var tracked = await new CarrierRepository(ctx).GetTrackedByIdAsync(carrier.Id);

        tracked.Should().NotBeNull();
        ctx.Entry(tracked!).State.Should().Be(Microsoft.EntityFrameworkCore.EntityState.Unchanged);
    }

    [Fact]
    public async Task UpdateAsync_persists_editable_fields_without_touching_metrics()
    {
        var carrier = TestData.Carrier(cnpj: "55666777000155", companyName: "Trans Antiga", averageRating: 4.5);
        var seed = _db.CreateContext();
        await new CarrierRepository(seed).AddAsync(carrier);
        await seed.SaveChangesAsync();

        var ctx = _db.CreateContext();
        var repo = new CarrierRepository(ctx);
        var tracked = await repo.GetTrackedByIdAsync(carrier.Id);
        tracked!.CompanyName = "Trans Nova";
        tracked.TradeName = "Fantasia Nova";
        await repo.UpdateAsync(tracked);

        var reloaded = await new CarrierRepository(_db.CreateContext()).GetByIdAsync(carrier.Id);
        reloaded!.CompanyName.Should().Be("Trans Nova");
        reloaded.TradeName.Should().Be("Fantasia Nova");
        reloaded.AverageRating.Should().Be(4.5);
        reloaded.OnTimeDeliveryRate.Should().Be(95);
    }
}
