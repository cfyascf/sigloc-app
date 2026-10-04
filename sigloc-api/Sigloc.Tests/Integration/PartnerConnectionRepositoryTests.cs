using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class PartnerConnectionRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static PartnerConnection Connection(Guid contractorId, Guid carrierId, PartnershipStatus status = PartnershipStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        ContractorId = contractorId,
        CarrierId = carrierId,
        Status = status,
        InitiatedBy = PartnershipInitiator.Contractor,
    };

    [Fact]
    public async Task AddAsync_does_not_save_until_caller_commits()
    {
        var contractor = TestData.Contractor(cnpj: "11111111000111");
        var carrier = TestData.Carrier(cnpj: "22222222000122");

        var seedCtx = _db.CreateContext();
        seedCtx.Contractors.Add(contractor);
        seedCtx.Carriers.Add(carrier);
        await seedCtx.SaveChangesAsync();

        var ctx = _db.CreateContext();
        await new PartnerConnectionRepository(ctx).AddAsync(Connection(contractor.Id, carrier.Id));

        (await new PartnerConnectionRepository(_db.CreateContext()).ExistsAsync(contractor.Id, carrier.Id)).Should().BeFalse();

        await ctx.SaveChangesAsync();

        (await new PartnerConnectionRepository(_db.CreateContext()).ExistsAsync(contractor.Id, carrier.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_matches_exact_pair()
    {
        var contractor = TestData.Contractor(cnpj: "33333333000133");
        var carrier = TestData.Carrier(cnpj: "44444444000144");

        var ctx = _db.CreateContext();
        ctx.Contractors.Add(contractor);
        ctx.Carriers.Add(carrier);
        ctx.PartnerConnections.Add(Connection(contractor.Id, carrier.Id));
        await ctx.SaveChangesAsync();

        var repo = new PartnerConnectionRepository(_db.CreateContext());
        (await repo.ExistsAsync(contractor.Id, carrier.Id)).Should().BeTrue();
        (await repo.ExistsAsync(contractor.Id, Guid.NewGuid())).Should().BeFalse();
        (await repo.ExistsAsync(Guid.NewGuid(), carrier.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetByContractorAsync_returns_connections_with_carrier_included()
    {
        var contractor = TestData.Contractor(cnpj: "55555555000155");
        var otherContractor = TestData.Contractor(cnpj: "55555555000156");
        var carrierA = TestData.Carrier(cnpj: "66666666000166", companyName: "Carrier A");
        var carrierB = TestData.Carrier(cnpj: "66666666000167", companyName: "Carrier B");

        var ctx = _db.CreateContext();
        ctx.Contractors.AddRange(contractor, otherContractor);
        ctx.Carriers.AddRange(carrierA, carrierB);
        ctx.PartnerConnections.AddRange(
            Connection(contractor.Id, carrierA.Id),
            Connection(otherContractor.Id, carrierB.Id));
        await ctx.SaveChangesAsync();

        var result = (await new PartnerConnectionRepository(_db.CreateContext())
            .GetByContractorAsync(contractor.Id)).ToList();

        result.Should().ContainSingle();
        result[0].Carrier.Should().NotBeNull();
        result[0].Carrier!.CompanyName.Should().Be("Carrier A");
    }

    [Fact]
    public async Task GetByCarrierAsync_returns_connections_with_contractor_included()
    {
        var contractorA = TestData.Contractor(cnpj: "77777777000177", companyName: "Contractor A");
        var contractorB = TestData.Contractor(cnpj: "77777777000178", companyName: "Contractor B");
        var carrier = TestData.Carrier(cnpj: "88888888000188");

        var ctx = _db.CreateContext();
        ctx.Contractors.AddRange(contractorA, contractorB);
        ctx.Carriers.Add(carrier);
        ctx.PartnerConnections.AddRange(
            Connection(contractorA.Id, carrier.Id),
            Connection(contractorB.Id, carrier.Id));
        await ctx.SaveChangesAsync();

        var result = (await new PartnerConnectionRepository(_db.CreateContext())
            .GetByCarrierAsync(carrier.Id)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Contractor != null);
    }
}
