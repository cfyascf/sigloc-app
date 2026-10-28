using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Constants;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class ProfileServiceTests
{
    private readonly IContractorRepository _contractors = Substitute.For<IContractorRepository>();
    private readonly ICarrierRepository _carriers = Substitute.For<ICarrierRepository>();
    private readonly ProfileService _service;
    private readonly Guid _companyId = Guid.NewGuid();

    public ProfileServiceTests()
    {
        _service = new ProfileService(_contractors, _carriers);
    }

    // ---------------- GetAsync ----------------

    [Fact]
    public async Task GetAsync_returns_contractor_profile_without_carrier_metrics()
    {
        var contractor = TestData.Contractor(id: _companyId, cnpj: "11222333000181", companyName: "Contratante Ltda");
        contractor.TradeName = "Contratante Fantasia";
        _contractors.GetByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.GetAsync(_companyId, isCarrier: false);

        result.Id.Should().Be(_companyId);
        result.Role.Should().Be(Roles.Shipper);
        result.Cnpj.Should().Be("11222333000181");
        result.CompanyName.Should().Be("Contratante Ltda");
        result.TradeName.Should().Be("Contratante Fantasia");
        result.AverageRating.Should().BeNull();
        result.HasActiveInsurancePolicy.Should().BeNull();
        result.OnTimeDeliveryRate.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_returns_carrier_profile_with_metrics()
    {
        var carrier = TestData.Carrier(id: _companyId, cnpj: "99888777000166", companyName: "Transportadora Ltda", averageRating: 4.7);
        carrier.TradeName = "Trans Fantasia";
        _carriers.GetByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(carrier);

        var result = await _service.GetAsync(_companyId, isCarrier: true);

        result.Role.Should().Be(Roles.Carrier);
        result.CompanyName.Should().Be("Transportadora Ltda");
        result.TradeName.Should().Be("Trans Fantasia");
        result.AverageRating.Should().Be(4.7);
        result.HasActiveInsurancePolicy.Should().BeTrue();
        result.OnTimeDeliveryRate.Should().Be(95);
    }

    [Fact]
    public async Task GetAsync_contractor_missing_throws_CompanyNotFound()
    {
        _contractors.GetByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var act = () => _service.GetAsync(_companyId, isCarrier: false);

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task GetAsync_carrier_missing_throws_CompanyNotFound()
    {
        _carriers.GetByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns((Carrier?)null);

        var act = () => _service.GetAsync(_companyId, isCarrier: true);

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task GetAsync_returns_contractor_logistics_contact()
    {
        var contractor = TestData.Contractor(id: _companyId);
        contractor.LogisticsContactName = "Mesa Logística";
        contractor.LogisticsContactPhone = "(41) 99999-0000";
        contractor.LogisticsContactEmail = "logistica@empresa.com.br";
        _contractors.GetByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.GetAsync(_companyId, isCarrier: false);

        result.LogisticsContactName.Should().Be("Mesa Logística");
        result.LogisticsContactPhone.Should().Be("(41) 99999-0000");
        result.LogisticsContactEmail.Should().Be("logistica@empresa.com.br");
    }

    // ---------------- UpdateAsync ----------------

    [Fact]
    public async Task UpdateAsync_sets_trims_keeps_and_clears_logistics_contact()
    {
        var contractor = TestData.Contractor(id: _companyId);
        contractor.LogisticsContactName = "Antigo";
        contractor.LogisticsContactPhone = "41999990000";
        contractor.LogisticsContactEmail = "antigo@empresa.com";
        _contractors.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.UpdateAsync(_companyId, isCarrier: false,
            new UpdateCompanyProfileDto("Razão", null, "  Mesa Logística  ", null, "  "));

        contractor.LogisticsContactName.Should().Be("Mesa Logística");
        contractor.LogisticsContactPhone.Should().Be("41999990000");
        contractor.LogisticsContactEmail.Should().BeNull();
        result.LogisticsContactName.Should().Be("Mesa Logística");
    }

    [Theory]
    [InlineData(151, 0, "razaoSocial")]
    [InlineData(10, 151, "nomeFantasia")]
    public async Task UpdateAsync_too_long_names_throw_Validation(int companyLength, int tradeLength, string field)
    {
        _contractors.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(TestData.Contractor(id: _companyId));

        var act = () => _service.UpdateAsync(_companyId, isCarrier: false,
            new UpdateCompanyProfileDto(new string('R', companyLength), tradeLength == 0 ? null : new string('T', tradeLength), null, null, null));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == field);
    }

    [Theory]
    [InlineData("12345", null, "logisticsContactPhone")]
    [InlineData("+55 41 9999-0000x", null, "logisticsContactPhone")]
    [InlineData(null, "sem-arroba", "logisticsContactEmail")]
    [InlineData(null, "a@localhost", "logisticsContactEmail")]
    public async Task UpdateAsync_invalid_logistics_contact_throws_Validation(string? phone, string? email, string field)
    {
        _contractors.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(TestData.Contractor(id: _companyId));

        var act = () => _service.UpdateAsync(_companyId, isCarrier: false,
            new UpdateCompanyProfileDto("Razão", null, null, phone, email));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == field);
    }

    [Fact]
    public async Task UpdateAsync_ignores_logistics_contact_for_carriers()
    {
        _carriers.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(TestData.Carrier(id: _companyId));

        var result = await _service.UpdateAsync(_companyId, isCarrier: true,
            new UpdateCompanyProfileDto("Razão", null, "Mesa", "123", "invalido"));

        result.LogisticsContactName.Should().BeNull();
        result.LogisticsContactPhone.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_updates_contractor_editable_fields()
    {
        var contractor = TestData.Contractor(id: _companyId, companyName: "Nome Antigo");
        contractor.TradeName = "Fantasia Antiga";
        _contractors.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.UpdateAsync(
            _companyId, isCarrier: false, new UpdateCompanyProfileDto("Nova Razão", "Novo Fantasia"));

        contractor.CompanyName.Should().Be("Nova Razão");
        contractor.TradeName.Should().Be("Novo Fantasia");
        result.CompanyName.Should().Be("Nova Razão");
        result.TradeName.Should().Be("Novo Fantasia");
        await _contractors.Received(1).UpdateAsync(contractor, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_updates_carrier_editable_fields()
    {
        var carrier = TestData.Carrier(id: _companyId);
        _carriers.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(carrier);

        var result = await _service.UpdateAsync(
            _companyId, isCarrier: true, new UpdateCompanyProfileDto("Nova Razão", "Novo Fantasia"));

        carrier.CompanyName.Should().Be("Nova Razão");
        carrier.TradeName.Should().Be("Novo Fantasia");
        result.CompanyName.Should().Be("Nova Razão");
        await _carriers.Received(1).UpdateAsync(carrier, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_trims_company_name_and_nullifies_blank_trade_name()
    {
        var contractor = TestData.Contractor(id: _companyId);
        _contractors.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.UpdateAsync(
            _companyId, isCarrier: false, new UpdateCompanyProfileDto("  Razão Trim  ", "   "));

        result.CompanyName.Should().Be("Razão Trim");
        result.TradeName.Should().BeNull();
        contractor.TradeName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_blank_company_name_throws_Validation_on_razaoSocial(string? companyName)
    {
        var act = () => _service.UpdateAsync(
            _companyId, isCarrier: false, new UpdateCompanyProfileDto(companyName, "Fantasia"));

        var assertion = await act.Should().ThrowAsync<ValidationException>();
        assertion.Which.Errors.Should().ContainSingle(e => e.Field == "razaoSocial");
        await _contractors.DidNotReceive().UpdateAsync(Arg.Any<Contractor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_contractor_missing_throws_CompanyNotFound()
    {
        _contractors.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var act = () => _service.UpdateAsync(
            _companyId, isCarrier: false, new UpdateCompanyProfileDto("Razão", "Fantasia"));

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_carrier_missing_throws_CompanyNotFound()
    {
        _carriers.GetTrackedByIdAsync(_companyId, Arg.Any<CancellationToken>()).Returns((Carrier?)null);

        var act = () => _service.UpdateAsync(
            _companyId, isCarrier: true, new UpdateCompanyProfileDto("Razão", "Fantasia"));

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
