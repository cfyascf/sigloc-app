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
}
