using System.Net;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class PartnersControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public async Task GetNetwork_without_authentication_is_401()
    {
        var service = Substitute.For<IPartnerNetworkService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.GetAsync("/api/partnerships");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNetwork_as_shipper_returns_contractor_network()
    {
        var service = Substitute.For<IPartnerNetworkService>();
        service.GetNetworkAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new PartnerNetworkDto(0, 0, new List<PartnerDto>()));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/partnerships");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetNetworkAsync(CompanyId, Arg.Any<CancellationToken>());
        await service.DidNotReceiveWithAnyArgs().GetCarrierNetworkAsync(default, default);
    }

    [Fact]
    public async Task GetNetwork_as_carrier_returns_carrier_network()
    {
        var service = Substitute.For<IPartnerNetworkService>();
        service.GetCarrierNetworkAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new CarrierNetworkDto(0, 0, new List<CarrierNetworkPartnerDto>()));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.GetAsync("/api/partnerships");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetCarrierNetworkAsync(CompanyId, Arg.Any<CancellationToken>());
        await service.DidNotReceiveWithAnyArgs().GetNetworkAsync(default, default);
    }
}
