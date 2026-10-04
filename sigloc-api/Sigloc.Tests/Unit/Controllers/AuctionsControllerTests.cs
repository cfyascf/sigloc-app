using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class AuctionsControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static CreateAuctionRequestDto CreateDto() =>
        new(SegmentIds: new[] { Guid.NewGuid() }, ExpiresAt: DateTimeOffset.UtcNow.AddDays(1), AutomaticAward: false);

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Carrier, 403)]
    [InlineData(Roles.Shipper, 201)]
    public async Task Create_enforces_shipper_policy_and_returns_created(string? role, int status)
    {
        var service = Substitute.For<IAuctionService>();
        service.CreateAsync(CompanyId, Arg.Any<CreateAuctionRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Auction());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/auctions", CreateDto());

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).CreateAsync(CompanyId, Arg.Any<CreateAuctionRequestDto>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Search_returns_ok_and_passes_identity()
    {
        var service = Substitute.For<IAuctionService>();
        service.SearchAsync(CompanyId, Arg.Any<AuctionQueryDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<PagedAuctionsDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/auctions?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).SearchAsync(CompanyId, Arg.Any<AuctionQueryDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        service.GetDetailAsync(CompanyId, id, Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<AuctionDetailDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync($"/api/auctions/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetDetailAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        service.UpdateAsync(CompanyId, id, Arg.Any<UpdateAuctionRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<UpdateAuctionResponseDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.PutAsJsonAsync($"/api/auctions/{id}",
            new UpdateAuctionRequestDto(ExpiresAt: DateTimeOffset.UtcNow.AddDays(2)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).UpdateAsync(CompanyId, id, Arg.Any<UpdateAuctionRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_no_content_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.DeleteAsync($"/api/auctions/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).DeleteAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBidRanking_returns_ok_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        service.GetBidRankingAsync(CompanyId, id, Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<BidRankingDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync($"/api/auctions/{id}/bids");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetBidRankingAsync(CompanyId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Award_returns_created_and_passes_identity()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.PostAsJsonAsync($"/api/auctions/{id}/award",
            new AwardAuctionRequestDto(WinningBidId: Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await service.Received(1).AwardAsync(CompanyId, id, Arg.Any<AwardAuctionRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Carrier, 200)]
    public async Task GetCarrierAnalysis_enforces_carrier_policy(string? role, int status)
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        service.GetCarrierAnalysisAsync(CompanyId, id, Arg.Any<CancellationToken>())
            .Returns(ControllerDtoFactory.Stub<CarrierBidAnalysisDto>());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.GetAsync($"/api/auctions/{id}/carrier-analysis");

        ((int)response.StatusCode).Should().Be(status);
        if (status == 200)
        {
            await service.Received(1).GetCarrierAnalysisAsync(CompanyId, id, Arg.Any<CancellationToken>());
        }
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Carrier, 201)]
    public async Task PlaceBid_enforces_carrier_policy_and_returns_created(string? role, int status)
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAuctionService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync($"/api/auctions/{id}/bids",
            new PlaceBidRequestDto(ValorOferecido: 1000m, VeiculoId: Guid.NewGuid()));

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).PlaceBidAsync(CompanyId, id, Arg.Any<PlaceBidRequestDto>(), Arg.Any<CancellationToken>());
        }
    }
}
