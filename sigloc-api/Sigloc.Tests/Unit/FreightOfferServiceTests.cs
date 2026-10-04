using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class FreightOfferServiceTests
{
    private readonly IAuctionRepository _auctionRepository = Substitute.For<IAuctionRepository>();
    private readonly IBidRepository _bidRepository = Substitute.For<IBidRepository>();
    private readonly FreightOfferService _service;

    private readonly Guid _carrierId = Guid.NewGuid();

    public FreightOfferServiceTests()
        => _service = new FreightOfferService(_auctionRepository, _bidRepository);

    // ---------------------------------------------------------------------
    // Builders
    // ---------------------------------------------------------------------

    private static RouteSegment Segment(
        DateTimeOffset? pickup = null,
        DateTimeOffset? delivery = null,
        params Product[] products)
    {
        var seg = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = Guid.NewGuid(),
            OriginAddress = "Curitiba, PR",
            DestinationAddress = "São Paulo, SP",
            OriginCoordinate = "-49.27,-25.42",
            DestinationCoordinate = "-46.63,-23.55",
            DistanceKm = 400,
            EstimatedTimeHours = 6,
            BudgetCeiling = 1000m,
            EstimatedTollCost = 50m,
            PickupDeadline = pickup ?? DateTimeOffset.UtcNow.AddDays(1),
            DeliveryDeadline = delivery ?? DateTimeOffset.UtcNow.AddDays(2),
            Status = SegmentStatus.Routed,
        };

        foreach (var p in products)
        {
            seg.Items.Add(new ProductRouteSegment
            {
                Id = Guid.NewGuid(),
                RouteSegmentId = seg.Id,
                ProductId = p.Id,
                Quantity = 1,
                Product = p,
            });
        }

        return seg;
    }

    private static ConsolidatedRoute Route(
        decimal ceiling = 5000m,
        double weightKg = 1500,
        double volumeM3 = 10,
        double distanceKm = 400) => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = Guid.NewGuid(),
            Status = RouteStatus.InAuction,
            TotalDistanceKm = distanceKm,
            ConsolidatedCeiling = ceiling,
            EstimatedAnttFloor = ceiling * 0.8m,
            TotalWeightKg = weightKg,
            TotalVolumeM3 = volumeM3,
        };

    private static Auction OpenAuction(DateTimeOffset? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        RouteId = Guid.NewGuid(),
        OpenedAt = DateTimeOffset.UtcNow.AddHours(-1),
        ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(2),
        Status = AuctionStatus.Open,
    };

    // ---------------------------------------------------------------------
    // ListAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ListAsync_maps_items_and_returns_paged_result()
    {
        var auction = OpenAuction();
        var route = Route();
        var segment = Segment(products: TestData.Product());
        var item = new CarrierAuctionWithRoute(auction, route, new[] { segment }, "Contratante X");

        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));

        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AuctionBidMetrics(auction.Id, 2000m, 3) });
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus> { [auction.Id] = BidStatus.Pending });

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, null, null));

        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().ContainSingle();
        var dto = result.Items[0];
        dto.Id.Should().Be(auction.Id.ToString());
        dto.Contractor.Should().Be("Contratante X");
        dto.TargetValue.Should().Be(route.ConsolidatedCeiling);
        dto.TotalBids.Should().Be(3);
        dto.BidStatus.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAsync_clamps_page_and_pagesize()
    {
        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 100, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<CarrierAuctionWithRoute>(), 0));

        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, null, null, Page: -5, PageSize: 1000));

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        await _auctionRepository.Received(1).SearchAvailableForCarrierAsync(
            _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_defaults_pagesize_when_zero()
    {
        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<CarrierAuctionWithRoute>(), 0));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, null, null, Page: 1, PageSize: 0));

        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListAsync_trims_search_and_passes_it_to_repository()
    {
        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, "curitiba", Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<CarrierAuctionWithRoute>(), 0));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        await _service.ListAsync(_carrierId, new FreightOfferQueryDto("  curitiba  ", null, null));

        await _auctionRepository.Received(1).SearchAvailableForCarrierAsync(
            _carrierId, "curitiba", Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_applies_risk_filter_excluding_non_matching()
    {
        // Normal-risk auction, far in the future with bids -> NORMAL.
        var auction = OpenAuction(expiresAt: DateTimeOffset.UtcNow.AddDays(5));
        var route = Route();
        var segment = Segment(
            pickup: DateTimeOffset.UtcNow.AddDays(3),
            delivery: DateTimeOffset.UtcNow.AddDays(4),
            products: TestData.Product());
        var item = new CarrierAuctionWithRoute(auction, route, new[] { segment }, "C");

        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AuctionBidMetrics(auction.Id, 2000m, 2) });
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, "CRITIC", null));

        // Item is NORMAL risk so a CRITIC filter removes it, but total (pre-filter) stays.
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task ListAsync_maps_null_bidstatus_when_carrier_has_no_bid()
    {
        var auction = OpenAuction();
        var item = new CarrierAuctionWithRoute(auction, Route(), new[] { Segment(products: TestData.Product()) }, "C");

        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, null, null));

        result.Items[0].BidStatus.Should().BeNull();
        result.Items[0].TotalBids.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_builds_requirement_tags_from_products()
    {
        var frozen = TestData.Product();
        frozen.TransportEnvironment = TransportEnvironment.Frozen;
        frozen.Dangerous = true;
        frozen.Fragile = true;

        var auction = OpenAuction();
        var item = new CarrierAuctionWithRoute(auction, Route(), new[] { Segment(products: frozen) }, "C");

        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, null, null));

        result.Items[0].Requirements.Should().Contain(new[] { "Congelado", "Perigoso", "Frágil" });
    }

    [Fact]
    public async Task ListAsync_requirement_tag_defaults_to_carga_seca_without_products()
    {
        var auction = OpenAuction();
        var item = new CarrierAuctionWithRoute(auction, Route(), new[] { Segment() }, "C");

        _auctionRepository.SearchAvailableForCarrierAsync(
                _carrierId, null, Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetCarrierBidStatusesAsync(_carrierId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, BidStatus>());

        var result = await _service.ListAsync(_carrierId, new FreightOfferQueryDto(null, null, null));

        result.Items[0].Requirements.Should().ContainSingle().Which.Should().Be("Carga Seca");
    }

    // ---------------------------------------------------------------------
    // GetByIdAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_returns_detail_when_available()
    {
        var auction = OpenAuction();
        var route = Route(ceiling: 7777m);
        var segment = Segment(products: TestData.Product());
        var detail = new CarrierAuctionWithRoute(auction, route, new[] { segment }, "Contratante Y");

        _auctionRepository.GetAvailableForCarrierAsync(auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(detail);
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AuctionBidMetrics(auction.Id, 5000m, 4) });

        var result = await _service.GetByIdAsync(_carrierId, auction.Id);

        result.Id.Should().Be(auction.Id.ToString());
        result.Contractor.Should().Be("Contratante Y");
        result.TargetValue.Should().Be(7777m);
        result.TotalBids.Should().Be(4);
        result.Details.Should().Contain("Distância total");
        result.SegmentId.Should().Be(segment.Id.ToString());
    }

    [Fact]
    public async Task GetByIdAsync_throws_OfferNotFound_when_missing()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetAvailableForCarrierAsync(id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((CarrierAuctionWithRoute?)null);

        var act = () => _service.GetByIdAsync(_carrierId, id);

        (await act.Should().ThrowAsync<OfferNotFoundException>())
            .Which.Message.Should().Contain(id.ToString());
        await _bidRepository.DidNotReceive().GetMetricsForAuctionsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_defaults_total_bids_when_no_metrics()
    {
        var auction = OpenAuction();
        var detail = new CarrierAuctionWithRoute(auction, Route(), new[] { Segment(products: TestData.Product()) }, "C");

        _auctionRepository.GetAvailableForCarrierAsync(auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(detail);
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());

        var result = await _service.GetByIdAsync(_carrierId, auction.Id);

        result.TotalBids.Should().Be(0);
    }
}
