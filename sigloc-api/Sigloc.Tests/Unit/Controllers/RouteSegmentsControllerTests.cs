using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class RouteSegmentsControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static RouteSegmentRequestDto RequestDto() => new(
        Origin: "Curitiba, PR",
        Destination: "São Paulo, SP",
        BudgetCeiling: 5000m,
        EstimatedTollCost: 120m,
        PickupDeadline: DateTimeOffset.UtcNow.AddDays(1),
        DeliveryDeadline: DateTimeOffset.UtcNow.AddDays(2),
        Items: new List<RouteSegmentItemRequestDto>());

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Carrier, 403)]
    [InlineData(Roles.Shipper, 201)]
    public async Task Create_enforces_shipper_policy_and_returns_created(string? role, int status)
    {
        var service = Substitute.For<IRouteSegmentService>();
        service.CreateAsync(CompanyId, Arg.Any<RouteSegmentRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.RouteSegment());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/route-segments", RequestDto());

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).CreateAsync(CompanyId, Arg.Any<RouteSegmentRequestDto>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetById_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IRouteSegmentService>();
        service.GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>()).Returns(ControllerDtoFactory.RouteSegment(id));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync($"/api/route-segments/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_returns_ok_and_passes_identity()
    {
        var service = Substitute.For<IRouteSegmentService>();
        service.SearchAsync(CompanyId, Arg.Any<RouteSegmentQueryDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<PagedRouteSegmentsDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/route-segments?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).SearchAsync(CompanyId, Arg.Any<RouteSegmentQueryDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IRouteSegmentService>();
        service.UpdateAsync(CompanyId, id, Arg.Any<RouteSegmentRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.RouteSegment(id));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.PutAsJsonAsync($"/api/route-segments/{id}", RequestDto());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).UpdateAsync(CompanyId, id, Arg.Any<RouteSegmentRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_no_content_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IRouteSegmentService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.DeleteAsync($"/api/route-segments/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).DeleteAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }
}
