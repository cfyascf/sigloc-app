using System.Net;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class DashboardControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Carrier, 403)]
    [InlineData(Roles.Shipper, 200)]
    public async Task GetExecutive_enforces_shipper_policy_and_passes_identity(string? role, int status)
    {
        var service = Substitute.For<IDashboardService>();
        service.GetExecutiveDashboardAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<DashboardExecutivoDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.GetAsync("/api/dashboard/executivo");

        ((int)response.StatusCode).Should().Be(status);
        if (status == 200)
        {
            await service.Received(1).GetExecutiveDashboardAsync(CompanyId, Arg.Any<CancellationToken>());
        }
        else
        {
            await service.DidNotReceiveWithAnyArgs().GetExecutiveDashboardAsync(default, default);
        }
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Carrier, 200)]
    public async Task GetCarrier_enforces_carrier_policy_and_passes_identity(string? role, int status)
    {
        var service = Substitute.For<IDashboardService>();
        service.GetCarrierDashboardAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<DashboardTransportadorDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.GetAsync("/api/dashboard/transportador");

        ((int)response.StatusCode).Should().Be(status);
        if (status == 200)
        {
            await service.Received(1).GetCarrierDashboardAsync(CompanyId, Arg.Any<CancellationToken>());
        }
        else
        {
            await service.DidNotReceiveWithAnyArgs().GetCarrierDashboardAsync(default, default);
        }
    }
}
