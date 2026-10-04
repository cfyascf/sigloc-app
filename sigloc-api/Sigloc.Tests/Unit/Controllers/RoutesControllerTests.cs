using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class RoutesControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Carrier, 403)]
    [InlineData(Roles.Shipper, 200)]
    public async Task Preview_enforces_shipper_policy_and_passes_identity(string? role, int status)
    {
        var service = Substitute.For<IRoutePreviewService>();
        service.PreviewAsync(CompanyId, Arg.Any<RoutePreviewRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<RoutePreviewResponseDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/routes/preview",
            new RoutePreviewRequestDto(new[] { Guid.NewGuid() }));

        ((int)response.StatusCode).Should().Be(status);
        if (status == 200)
        {
            await service.Received(1).PreviewAsync(CompanyId, Arg.Any<RoutePreviewRequestDto>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await service.DidNotReceiveWithAnyArgs().PreviewAsync(default, default!, default);
        }
    }
}
