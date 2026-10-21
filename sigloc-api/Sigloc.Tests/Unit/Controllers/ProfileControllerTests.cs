using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class ProfileControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static CompanyProfileDto ShipperProfile() =>
        new(CompanyId, Roles.Shipper, "11222333000181", "Contratante Ltda", "Fantasia", null, null, null);

    private static CompanyProfileDto CarrierProfile() =>
        new(CompanyId, Roles.Carrier, "99888777000166", "Transportadora Ltda", "Fantasia", 4.5, true, 95);

    [Fact]
    public async Task Get_without_authentication_is_401()
    {
        var service = Substitute.For<IProfileService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_as_shipper_passes_isCarrier_false()
    {
        var service = Substitute.For<IProfileService>();
        service.GetAsync(CompanyId, false, Arg.Any<CancellationToken>()).Returns(ShipperProfile());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetAsync(CompanyId, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_as_carrier_passes_isCarrier_true()
    {
        var service = Substitute.For<IProfileService>();
        service.GetAsync(CompanyId, true, Arg.Any<CancellationToken>()).Returns(CarrierProfile());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetAsync(CompanyId, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_passes_dto_and_returns_200()
    {
        var service = Substitute.For<IProfileService>();
        service.UpdateAsync(CompanyId, false, Arg.Any<UpdateCompanyProfileDto>(), Arg.Any<CancellationToken>())
            .Returns(ShipperProfile());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.PutAsJsonAsync(
            "/api/profile", new UpdateCompanyProfileDto("Nova Razão", "Novo Fantasia"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).UpdateAsync(
            CompanyId,
            false,
            Arg.Is<UpdateCompanyProfileDto>(d => d.CompanyName == "Nova Razão" && d.TradeName == "Novo Fantasia"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_blank_company_name_is_400()
    {
        var service = Substitute.For<IProfileService>();
        service.UpdateAsync(CompanyId, false, Arg.Any<UpdateCompanyProfileDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CompanyProfileDto>(
                new ValidationException(new[] { new ValidationError("razaoSocial", "Campo obrigatório.") }, "Erro.")));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.PutAsJsonAsync(
            "/api/profile", new UpdateCompanyProfileDto("", "Fantasia"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_company_missing_is_404()
    {
        var service = Substitute.For<IProfileService>();
        service.UpdateAsync(CompanyId, true, Arg.Any<UpdateCompanyProfileDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CompanyProfileDto>(new CompanyNotFoundException(CompanyId)));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.PutAsJsonAsync(
            "/api/profile", new UpdateCompanyProfileDto("Razão", "Fantasia"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
