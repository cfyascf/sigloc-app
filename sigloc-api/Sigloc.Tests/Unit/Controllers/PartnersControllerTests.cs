using System.Net;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
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

    [Fact]
    public async Task Remove_without_authentication_is_401()
    {
        var service = Substitute.For<IPartnerNetworkService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.DeleteAsync($"/api/partnerships/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Remove_as_party_returns_204_and_calls_service()
    {
        var connectionId = Guid.NewGuid();
        var service = Substitute.For<IPartnerNetworkService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.DeleteAsync($"/api/partnerships/{connectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).RemovePartnershipAsync(CompanyId, connectionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_as_carrier_returns_204_and_calls_service()
    {
        var connectionId = Guid.NewGuid();
        var service = Substitute.For<IPartnerNetworkService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.DeleteAsync($"/api/partnerships/{connectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).RemovePartnershipAsync(CompanyId, connectionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_unknown_connection_is_404()
    {
        var connectionId = Guid.NewGuid();
        var service = Substitute.For<IPartnerNetworkService>();
        service.RemovePartnershipAsync(CompanyId, connectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new PartnershipNotFoundException(connectionId)));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.DeleteAsync($"/api/partnerships/{connectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
