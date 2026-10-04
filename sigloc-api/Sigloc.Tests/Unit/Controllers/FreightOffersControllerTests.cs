using System.Net;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class FreightOffersControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Carrier, 200)]
    public async Task List_enforces_carrier_policy_and_passes_identity(string? role, int status)
    {
        var service = Substitute.For<IFreightOfferService>();
        service.ListAsync(CompanyId, Arg.Any<FreightOfferQueryDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<PagedFreightOffersDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.GetAsync("/api/freight-offers?page=1&pageSize=20");

        ((int)response.StatusCode).Should().Be(status);
        if (status == 200)
        {
            await service.Received(1).ListAsync(CompanyId, Arg.Any<FreightOfferQueryDto>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await service.DidNotReceiveWithAnyArgs().ListAsync(default, default!, default);
        }
    }

    [Fact]
    public async Task GetById_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IFreightOfferService>();
        service.GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<FreightOfferDetailDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.GetAsync($"/api/freight-offers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }
}
