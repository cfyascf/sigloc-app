using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class ProductsControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static ProductRequestDto RequestDto() => new(
        Sku: "SKU-001", Name: "Caixa", Type: null, Category: "General", TransportEnvironment: "Dry",
        TempMin: null, TempMax: null, PackagingType: null, Dangerous: false, Fragile: false,
        DefaultWeight: 10, DefaultVolume: 1, HandlingRestriction: null);

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Carrier, 403)]
    [InlineData(Roles.Shipper, 201)]
    public async Task Create_enforces_shipper_policy_and_returns_created(string? role, int status)
    {
        var service = Substitute.For<IProductService>();
        service.CreateAsync(CompanyId, Arg.Any<ProductRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Product());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/products", RequestDto());

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).CreateAsync(CompanyId, Arg.Any<ProductRequestDto>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetById_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IProductService>();
        service.GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>()).Returns(ControllerDtoFactory.Product(id));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetByIdAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_returns_ok_and_passes_identity()
    {
        var service = Substitute.For<IProductService>();
        service.SearchAsync(CompanyId, Arg.Any<ProductQueryDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<PagedProductsDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/products?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).SearchAsync(CompanyId, Arg.Any<ProductQueryDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IProductService>();
        service.UpdateAsync(CompanyId, id, Arg.Any<ProductRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Product(id));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.PutAsJsonAsync($"/api/products/{id}", RequestDto());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).UpdateAsync(CompanyId, id, Arg.Any<ProductRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_no_content_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IProductService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.DeleteAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).DeleteAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }
}
