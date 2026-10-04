using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class PartnerNetworkServiceTests
{
    private readonly IPartnerConnectionRepository _connections = Substitute.For<IPartnerConnectionRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly PartnerNetworkService _service;
    private readonly Guid _contractorId = Guid.NewGuid();
    private readonly Guid _carrierId = Guid.NewGuid();

    public PartnerNetworkServiceTests()
    {
        _service = new PartnerNetworkService(_connections, _vehicles);
        _vehicles
            .CountFreeByCarrierIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
    }

    private static PartnerConnection ContractorSideConnection(
        Guid contractorId,
        Carrier? carrier,
        PartnershipStatus status = PartnershipStatus.Active)
        => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId,
            CarrierId = carrier?.Id ?? Guid.NewGuid(),
            Status = status,
            InitiatedBy = PartnershipInitiator.Contractor,
            Carrier = carrier,
            UpdatedAt = new DateTimeOffset(2030, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

    private static PartnerConnection CarrierSideConnection(
        Guid carrierId,
        Contractor? contractor,
        PartnershipStatus status = PartnershipStatus.Active)
        => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor?.Id ?? Guid.NewGuid(),
            CarrierId = carrierId,
            Status = status,
            InitiatedBy = PartnershipInitiator.Carrier,
            Contractor = contractor,
            UpdatedAt = new DateTimeOffset(2030, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

    // ---------------- GetNetworkAsync ----------------

    [Fact]
    public async Task GetNetworkAsync_maps_partners_counts_and_masks_cnpj()
    {
        var carrier = TestData.Carrier(id: _carrierId, cnpj: "12345678000199", companyName: "ACME", averageRating: 4.2);
        carrier.TradeName = "ACME Transportes";
        var active = ContractorSideConnection(_contractorId, carrier, PartnershipStatus.Active);
        var pending = ContractorSideConnection(_contractorId, TestData.Carrier(), PartnershipStatus.Pending);

        _connections
            .GetByContractorAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { active, pending });
        _vehicles
            .CountFreeByCarrierIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [_carrierId] = 7 });

        var result = await _service.GetNetworkAsync(_contractorId);

        result.TotalActive.Should().Be(1);
        result.TotalPending.Should().Be(1);
        result.Partners.Should().HaveCount(2);

        var mapped = result.Partners.Single(p => p.Carrier.Id == _carrierId);
        mapped.PartnershipStatus.Should().Be("ATIVA");
        mapped.Carrier.TradeName.Should().Be("ACME Transportes");
        mapped.Carrier.Cnpj.Should().Be("12.345.678/0001-99");
        mapped.Carrier.AverageRating.Should().Be(4.2);
        mapped.Carrier.HasActiveInsurancePolicy.Should().BeTrue();
        mapped.OperationalMetrics.FreeVehicles.Should().Be(7);
        mapped.OperationalMetrics.ActiveTripsWithUs.Should().Be(0);
        mapped.OperationalMetrics.LastInteraction.Should().Be(active.UpdatedAt);
    }

    [Fact]
    public async Task GetNetworkAsync_falls_back_to_company_name_when_no_trade_name()
    {
        var carrier = TestData.Carrier(id: _carrierId, companyName: "Razão Social LTDA");
        carrier.TradeName = null;
        _connections
            .GetByContractorAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { ContractorSideConnection(_contractorId, carrier) });

        var result = await _service.GetNetworkAsync(_contractorId);

        result.Partners.Single().Carrier.TradeName.Should().Be("Razão Social LTDA");
    }

    [Fact]
    public async Task GetNetworkAsync_null_carrier_nav_uses_fallback_name_and_empty_cnpj()
    {
        var connection = ContractorSideConnection(_contractorId, carrier: null);
        _connections
            .GetByContractorAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { connection });

        var result = await _service.GetNetworkAsync(_contractorId);

        var mapped = result.Partners.Single();
        mapped.Carrier.Id.Should().Be(connection.CarrierId);
        mapped.Carrier.TradeName.Should().Be("(transportadora não encontrada)");
        mapped.Carrier.Cnpj.Should().BeEmpty();
        mapped.Carrier.AverageRating.Should().BeNull();
        mapped.Carrier.HasActiveInsurancePolicy.Should().BeFalse();
    }

    [Fact]
    public async Task GetNetworkAsync_free_vehicles_defaults_to_zero_when_missing()
    {
        var carrier = TestData.Carrier(id: _carrierId);
        _connections
            .GetByContractorAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { ContractorSideConnection(_contractorId, carrier) });
        _vehicles
            .CountFreeByCarrierIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        var result = await _service.GetNetworkAsync(_contractorId);

        result.Partners.Single().OperationalMetrics.FreeVehicles.Should().Be(0);
    }

    [Fact]
    public async Task GetNetworkAsync_empty_returns_zero_counts()
    {
        _connections
            .GetByContractorAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection>());

        var result = await _service.GetNetworkAsync(_contractorId);

        result.TotalActive.Should().Be(0);
        result.TotalPending.Should().Be(0);
        result.Partners.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNetworkAsync_maps_rejected_status()
    {
        var carrier = TestData.Carrier(id: _carrierId);
        _connections
            .GetByContractorAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { ContractorSideConnection(_contractorId, carrier, PartnershipStatus.Rejected) });

        var result = await _service.GetNetworkAsync(_contractorId);

        result.Partners.Single().PartnershipStatus.Should().Be("RECUSADA");
        result.TotalActive.Should().Be(0);
        result.TotalPending.Should().Be(0);
    }

    // ---------------- GetCarrierNetworkAsync ----------------

    [Fact]
    public async Task GetCarrierNetworkAsync_maps_contractors_counts_and_masks_cnpj()
    {
        var contractor = TestData.Contractor(cnpj: "98765432000155", companyName: "Contratante SA");
        contractor.TradeName = "Contratante Fantasia";
        var active = CarrierSideConnection(_carrierId, contractor, PartnershipStatus.Active);
        var pending = CarrierSideConnection(_carrierId, TestData.Contractor(), PartnershipStatus.Pending);

        _connections
            .GetByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { active, pending });

        var result = await _service.GetCarrierNetworkAsync(_carrierId);

        result.TotalActive.Should().Be(1);
        result.TotalPending.Should().Be(1);
        result.Partners.Should().HaveCount(2);

        var mapped = result.Partners.Single(p => p.Contractor.Id == contractor.Id);
        mapped.PartnershipStatus.Should().Be("ATIVA");
        mapped.Contractor.TradeName.Should().Be("Contratante Fantasia");
        mapped.Contractor.Cnpj.Should().Be("98.765.432/0001-55");
        mapped.LastInteraction.Should().Be(active.UpdatedAt);
    }

    [Fact]
    public async Task GetCarrierNetworkAsync_null_contractor_nav_uses_fallback()
    {
        var connection = CarrierSideConnection(_carrierId, contractor: null);
        _connections
            .GetByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { connection });

        var result = await _service.GetCarrierNetworkAsync(_carrierId);

        var mapped = result.Partners.Single();
        mapped.Contractor.Id.Should().Be(connection.ContractorId);
        mapped.Contractor.TradeName.Should().Be("(contratante não encontrado)");
        mapped.Contractor.Cnpj.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCarrierNetworkAsync_falls_back_to_company_name()
    {
        var contractor = TestData.Contractor(companyName: "Somente Razão");
        contractor.TradeName = null;
        _connections
            .GetByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { CarrierSideConnection(_carrierId, contractor) });

        var result = await _service.GetCarrierNetworkAsync(_carrierId);

        result.Partners.Single().Contractor.TradeName.Should().Be("Somente Razão");
    }

    [Fact]
    public async Task GetCarrierNetworkAsync_malformed_cnpj_returned_as_is()
    {
        var contractor = TestData.Contractor(cnpj: "123");
        _connections
            .GetByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection> { CarrierSideConnection(_carrierId, contractor) });

        var result = await _service.GetCarrierNetworkAsync(_carrierId);

        result.Partners.Single().Contractor.Cnpj.Should().Be("123");
    }

    [Fact]
    public async Task GetCarrierNetworkAsync_empty_returns_zero_counts()
    {
        _connections
            .GetByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerConnection>());

        var result = await _service.GetCarrierNetworkAsync(_carrierId);

        result.TotalActive.Should().Be(0);
        result.TotalPending.Should().Be(0);
        result.Partners.Should().BeEmpty();
    }
}
