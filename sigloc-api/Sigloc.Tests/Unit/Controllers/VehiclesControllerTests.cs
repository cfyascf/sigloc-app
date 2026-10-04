using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using Sigloc.Domain.Enums;

namespace Sigloc.Tests.Unit.Controllers;

public class VehiclesControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static CreateVehicleDto CreateDto() => new(
        Plate: "ABC1D23",
        Model: "Volvo FH",
        AxleCount: 3,
        CapacityWeight: 20000m,
        CapacityVolume: 80m,
        BodyType: VehicleBodyType.Bau,
        RefrigerationLevel: RefrigerationLevel.Nenhuma,
        HasMopp: false,
        HasCargoSecuring: true,
        Driver: "João",
        CurrentLocation: "Curitiba, PR");

    private static UpdateVehicleDto UpdateDto() => new(
        Model: "Scania", AxleCount: null, CapacityWeight: null, CapacityVolume: null,
        BodyType: null, RefrigerationLevel: null, HasMopp: null, HasCargoSecuring: null,
        Driver: null, CurrentLocation: null, Status: null);

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Carrier, 201)]
    public async Task Create_enforces_carrier_policy_and_returns_created(string? role, int status)
    {
        var service = Substitute.For<IVehicleService>();
        service.CreateAsync(CompanyId, Arg.Any<CreateVehicleDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Vehicle());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/vehicles", CreateDto());

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).CreateAsync(CompanyId, Arg.Any<CreateVehicleDto>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await service.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default);
        }
    }

    [Fact]
    public async Task GetById_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IVehicleService>();
        service.GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>()).Returns(ControllerDtoFactory.Vehicle(id));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.GetAsync($"/api/vehicles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_returns_ok_and_passes_identity()
    {
        var service = Substitute.For<IVehicleService>();
        service.GetAllAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(Array.Empty<VehicleResponseDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.GetAsync("/api/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetAllAsync(CompanyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_no_content_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IVehicleService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.PutAsJsonAsync($"/api/vehicles/{id}", UpdateDto());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).UpdateAsync(CompanyId, id, Arg.Any<UpdateVehicleDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_no_content_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IVehicleService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Carrier);

        var response = await client.DeleteAsync($"/api/vehicles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).DeleteAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_without_authentication_is_401()
    {
        var service = Substitute.For<IVehicleService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.GetAsync("/api/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
