using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class AuctionServiceTests
{
    private readonly IRouteSegmentRepository _segmentRepository = Substitute.For<IRouteSegmentRepository>();
    private readonly IConsolidatedRouteRepository _routeRepository = Substitute.For<IConsolidatedRouteRepository>();
    private readonly IAuctionRepository _auctionRepository = Substitute.For<IAuctionRepository>();
    private readonly IBidRepository _bidRepository = Substitute.For<IBidRepository>();
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly IVehicleRepository _vehicleRepository = Substitute.For<IVehicleRepository>();
    private readonly IBlockedBidAttemptRepository _blockedBidAttemptRepository = Substitute.For<IBlockedBidAttemptRepository>();
    private readonly IRouteGeocodingService _geocodingService = Substitute.For<IRouteGeocodingService>();
    private readonly IAuctionNotifier _notifier = Substitute.For<IAuctionNotifier>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AuctionService _service;

    private readonly Guid _contractorId = Guid.NewGuid();
    private readonly Guid _carrierId = Guid.NewGuid();

    public AuctionServiceTests()
    {
        _service = new AuctionService(
            _segmentRepository,
            _routeRepository,
            _auctionRepository,
            _bidRepository,
            _tripRepository,
            _vehicleRepository,
            _blockedBidAttemptRepository,
            _geocodingService,
            _notifier,
            _unitOfWork);

        // Run the transactional delegate so inner logic executes.
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<(ConsolidatedRoute, Auction)>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task<(ConsolidatedRoute, Auction)>>>()(CancellationToken.None));
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Trip>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task<Trip>>>()(CancellationToken.None));
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task<bool>>>()(CancellationToken.None));
    }

    // ---------------------------------------------------------------------
    // Builders
    // ---------------------------------------------------------------------

    private static RouteSegment Segment(
        Guid? contractorId = null,
        SegmentStatus status = SegmentStatus.Available,
        Guid? routeId = null,
        DateTimeOffset? pickup = null,
        DateTimeOffset? delivery = null,
        decimal toll = 50m,
        params Product[] products)
    {
        var seg = new RouteSegment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId ?? Guid.NewGuid(),
            RouteId = routeId,
            OriginAddress = "Curitiba, PR",
            DestinationAddress = "São Paulo, SP",
            OriginCoordinate = "-49.27,-25.42",
            DestinationCoordinate = "-46.63,-23.55",
            DistanceKm = 400,
            EstimatedTimeHours = 6,
            BudgetCeiling = 1000m,
            EstimatedTollCost = toll,
            PickupDeadline = pickup ?? DateTimeOffset.UtcNow.AddDays(1),
            DeliveryDeadline = delivery ?? DateTimeOffset.UtcNow.AddDays(2),
            Status = status,
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

    private ConsolidatedRoute Route(
        decimal ceiling = 5000m,
        double weightKg = 1500,
        double volumeM3 = 10,
        double distanceKm = 400) => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = _contractorId,
            Status = RouteStatus.InAuction,
            TotalDistanceKm = distanceKm,
            ConsolidatedCeiling = ceiling,
            EstimatedAnttFloor = ceiling * 0.8m,
            TotalWeightKg = weightKg,
            TotalVolumeM3 = volumeM3,
        };

    private static Auction Auction(Guid? routeId = null, AuctionStatus status = AuctionStatus.Open) => new()
    {
        Id = Guid.NewGuid(),
        RouteId = routeId ?? Guid.NewGuid(),
        OpenedAt = DateTimeOffset.UtcNow.AddHours(-1),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(2),
        Status = status,
    };

    // =====================================================================
    // CreateAsync
    // =====================================================================

    [Fact]
    public async Task CreateAsync_empty_segment_ids_throws_Validation()
    {
        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: Array.Empty<Guid>(), ExpiresAt: DateTimeOffset.UtcNow.AddDays(1), AutomaticAward: false));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_null_expiry_throws_Validation()
    {
        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { Guid.NewGuid() }, ExpiresAt: null, AutomaticAward: false));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_past_expiry_throws_Validation()
    {
        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { Guid.NewGuid() }, ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-1), AutomaticAward: false));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_missing_segments_throws_RouteSegmentsNotFound()
    {
        var missingId = Guid.NewGuid();
        _segmentRepository.GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RouteSegment>());

        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { missingId }, ExpiresAt: DateTimeOffset.UtcNow.AddDays(1), AutomaticAward: false));

        await act.Should().ThrowAsync<RouteSegmentsNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_unavailable_segment_throws_SegmentUnavailable()
    {
        var seg = Segment(contractorId: _contractorId, status: SegmentStatus.Routed, routeId: Guid.NewGuid());
        _segmentRepository.GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), true, Arg.Any<CancellationToken>())
            .Returns(new[] { seg });

        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { seg.Id }, ExpiresAt: DateTimeOffset.UtcNow.AddHours(12), AutomaticAward: false));

        (await act.Should().ThrowAsync<SegmentUnavailableException>()).Which.SegmentId.Should().Be(seg.Id);
    }

    [Fact]
    public async Task CreateAsync_happy_path_persists_route_auction_and_notifies()
    {
        var product = TestData.Product();
        var seg = Segment(contractorId: _contractorId, status: SegmentStatus.Available, products: product);
        _segmentRepository.GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), true, Arg.Any<CancellationToken>())
            .Returns(new[] { seg });

        // Must close before the segment's first pickup (default: one day from now).
        var expiresAt = DateTimeOffset.UtcNow.AddHours(12);
        var result = await _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { seg.Id }, ExpiresAt: expiresAt, AutomaticAward: true));

        result.UpdatedSegments.Should().Be(1);
        result.Auction.Status.Should().Be(AuctionStatus.Open.ToWire());
        result.Auction.AutomaticAward.Should().BeTrue();

        seg.Status.Should().Be(SegmentStatus.Routed);
        seg.RouteId.Should().NotBeNull();
        seg.RouteSequence.Should().Be(1);

        await _routeRepository.Received(1).AddAsync(Arg.Any<ConsolidatedRoute>(), Arg.Any<CancellationToken>());
        await _auctionRepository.Received(1).AddAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).AuctionOpenedAsync(Arg.Any<Guid>(), _contractorId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_deadline_window_shorter_than_route_time_throws_Validation()
    {
        var pickup = DateTimeOffset.UtcNow.AddDays(1);
        var seg = Segment(
            contractorId: _contractorId,
            status: SegmentStatus.Available,
            pickup: pickup,
            delivery: pickup.AddHours(10),
            products: TestData.Product());
        seg.EstimatedTimeHours = 30; // 30h route needs more than the 10h window

        _segmentRepository.GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), true, Arg.Any<CancellationToken>())
            .Returns(new[] { seg });

        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { seg.Id }, ExpiresAt: DateTimeOffset.UtcNow.AddHours(12), AutomaticAward: false));

        var assertion = await act.Should().ThrowAsync<ValidationException>();
        assertion.Which.Errors.Should().ContainSingle(e => e.Field == "deliveryDeadline");
        await _auctionRepository.DidNotReceive().AddAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_deadline_window_equal_to_route_time_is_allowed()
    {
        var pickup = DateTimeOffset.UtcNow.AddDays(1);
        var seg = Segment(
            contractorId: _contractorId,
            status: SegmentStatus.Available,
            pickup: pickup,
            delivery: pickup.AddHours(8),
            products: TestData.Product());
        seg.EstimatedTimeHours = 8; // window exactly equals the route time

        _segmentRepository.GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), true, Arg.Any<CancellationToken>())
            .Returns(new[] { seg });

        var result = await _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { seg.Id }, ExpiresAt: DateTimeOffset.UtcNow.AddHours(12), AutomaticAward: false));

        result.Auction.Status.Should().Be(AuctionStatus.Open.ToWire());
        await _auctionRepository.Received(1).AddAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_expiry_not_before_first_pickup_throws_Validation()
    {
        var pickup = DateTimeOffset.UtcNow.AddDays(1);
        var seg = Segment(contractorId: _contractorId, status: SegmentStatus.Available, pickup: pickup, delivery: pickup.AddDays(1), products: TestData.Product());
        _segmentRepository.GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), true, Arg.Any<CancellationToken>())
            .Returns(new[] { seg });

        var act = () => _service.CreateAsync(_contractorId,
            new CreateAuctionRequestDto(SegmentIds: new[] { seg.Id }, ExpiresAt: pickup.AddHours(1), AutomaticAward: false));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().Contain(e => e.Field == "expiresAt");
        await _auctionRepository.DidNotReceive().AddAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // SearchAsync
    // =====================================================================

    [Fact]
    public async Task SearchAsync_maps_items_and_pages()
    {
        var auction = Auction();
        var route = Route();
        var seg = Segment(products: TestData.Product());
        var item = new AuctionWithRoute(auction, route, new[] { seg });

        _auctionRepository.SearchAsync(_contractorId, null, AuctionStatus.Open, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AuctionBidMetrics(auction.Id, 1000m, 2) });

        var result = await _service.SearchAsync(_contractorId, new AuctionQueryDto(null, null));

        result.TotalItems.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(auction.Id);
        result.Items[0].BidMetrics.TotalBids.Should().Be(2);
        result.Items[0].BidMetrics.BestBid.Should().Be(1000m);
    }

    [Fact]
    public async Task SearchAsync_parses_status_and_trims_search()
    {
        _auctionRepository.SearchAsync(_contractorId, "abc", AuctionStatus.Closed, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AuctionWithRoute>(), 0));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());

        await _service.SearchAsync(_contractorId, new AuctionQueryDto("  abc ", "closed"));

        await _auctionRepository.Received(1).SearchAsync(_contractorId, "abc", AuctionStatus.Closed, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_clamps_pagesize()
    {
        _auctionRepository.SearchAsync(_contractorId, null, AuctionStatus.Open, 1, 100, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AuctionWithRoute>(), 0));
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());

        var result = await _service.SearchAsync(_contractorId, new AuctionQueryDto(null, null, Page: 0, PageSize: 9999));

        result.PageSize.Should().Be(100);
        result.CurrentPage.Should().Be(1);
    }

    // =====================================================================
    // GetDetailAsync
    // =====================================================================

    [Fact]
    public async Task GetDetailAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetDetailAsync(id, _contractorId, Arg.Any<CancellationToken>())
            .Returns((AuctionWithRoute?)null);

        var act = () => _service.GetDetailAsync(_contractorId, id);

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task GetDetailAsync_returns_detail_with_best_bid()
    {
        var auction = Auction();
        var route = Route(ceiling: 9000m);
        var seg = Segment(products: TestData.Product());
        var detail = new AuctionWithRoute(auction, route, new[] { seg });

        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AuctionBidMetrics(auction.Id, 8000m, 5) });
        _bidRepository.GetBestBidWithCarrierAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(new BestBidWithCarrier(8000m, "Transportadora Z"));

        var result = await _service.GetDetailAsync(_contractorId, auction.Id);

        result.Id.Should().Be(auction.Id);
        result.BidMetrics.TotalBids.Should().Be(5);
        result.BidMetrics.BestBid!.Value.Should().Be(8000m);
        result.BidMetrics.BestBid.CarrierName.Should().Be("Transportadora Z");
        result.Segments.Should().ContainSingle();
        result.Route.FinancialScenario.ConsolidatedCeiling.Should().Be(9000m);
    }

    [Fact]
    public async Task GetDetailAsync_null_best_bid_maps_to_null()
    {
        var auction = Auction();
        var detail = new AuctionWithRoute(auction, Route(), new[] { Segment(products: TestData.Product()) });

        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetBestBidWithCarrierAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns((BestBidWithCarrier?)null);

        var result = await _service.GetDetailAsync(_contractorId, auction.Id);

        result.BidMetrics.BestBid.Should().BeNull();
        result.BidMetrics.TotalBids.Should().Be(0);
    }

    // =====================================================================
    // UpdateAsync
    // =====================================================================

    [Fact]
    public async Task UpdateAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetTrackedByIdAsync(id, _contractorId, Arg.Any<CancellationToken>())
            .Returns((Auction?)null);

        var act = () => _service.UpdateAsync(_contractorId, id, new UpdateAuctionRequestDto(DateTimeOffset.UtcNow.AddDays(1)));

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_updates_expiry_and_persists()
    {
        var auction = Auction();
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        var newExpiry = DateTimeOffset.UtcNow.AddDays(3);

        var result = await _service.UpdateAsync(_contractorId, auction.Id, new UpdateAuctionRequestDto(newExpiry));

        result.Id.Should().Be(auction.Id);
        result.ExpiresAt.Should().Be(newExpiry);
        auction.ExpiresAt.Should().Be(newExpiry);
        await _auctionRepository.Received(1).UpdateAsync(auction, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_past_expiry_throws_Validation()
    {
        var auction = Auction();
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);

        var act = () => _service.UpdateAsync(_contractorId, auction.Id, new UpdateAuctionRequestDto(DateTimeOffset.UtcNow.AddMinutes(-5)));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_expiry_after_first_pickup_throws_Validation_and_keeps_expiry()
    {
        var route = Route();
        var auction = Auction(routeId: route.Id);
        var originalExpiry = auction.ExpiresAt;
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        var pickup = DateTimeOffset.UtcNow.AddDays(1);
        var detail = new AuctionWithRoute(auction, route, new[] { Segment(pickup: pickup, delivery: pickup.AddDays(1)) });
        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);

        var act = () => _service.UpdateAsync(_contractorId, auction.Id, new UpdateAuctionRequestDto(pickup.AddDays(1)));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().Contain(e => e.Field == "expiresAt");
        auction.ExpiresAt.Should().Be(originalExpiry);
        await _auctionRepository.DidNotReceive().UpdateAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_null_expiry_keeps_existing_and_persists()
    {
        var auction = Auction();
        var original = auction.ExpiresAt;
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);

        var result = await _service.UpdateAsync(_contractorId, auction.Id, new UpdateAuctionRequestDto(null));

        result.ExpiresAt.Should().Be(original);
        await _auctionRepository.Received(1).UpdateAsync(auction, Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // DeleteAsync
    // =====================================================================

    [Fact]
    public async Task DeleteAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetTrackedByIdAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((Auction?)null);

        var act = () => _service.DeleteAsync(_contractorId, id);

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_soft_cancels_but_keeps_segments_consumed_when_found()
    {
        var route = Route();
        var auction = Auction(route.Id);
        var segment = Segment();
        segment.Status = SegmentStatus.Routed;
        segment.RouteId = route.Id;
        segment.RouteSequence = 1;

        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);
        _segmentRepository.GetTrackedByRouteAsync(route.Id, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { segment });
        var bidder = Guid.NewGuid();
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = bidder, Status = BidStatus.Pending },
            new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = Guid.NewGuid(), Status = BidStatus.Withdrawn },
        });

        await _service.DeleteAsync(_contractorId, auction.Id);

        // Only carriers with a live bid are told.
        await _notifier.Received(1).AuctionCancelledAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { bidder })),
            $"{segment.OriginAddress} → {segment.DestinationAddress}", Arg.Any<CancellationToken>());
        auction.Status.Should().Be(AuctionStatus.Cancelled);
        // Delete keeps the route and segments consumed; they are NOT released.
        route.Status.Should().Be(RouteStatus.InAuction);
        segment.Status.Should().Be(SegmentStatus.Routed);
        segment.RouteId.Should().Be(route.Id);
        segment.RouteSequence.Should().Be(1);
        await _auctionRepository.DidNotReceive().DeleteAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_throws_when_auction_not_open()
    {
        var auction = Auction(status: AuctionStatus.Closed);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);

        var act = () => _service.DeleteAsync(_contractorId, auction.Id);

        await act.Should().ThrowAsync<AuctionNotOpenException>();
    }

    // =====================================================================
    // UndoAsync
    // =====================================================================

    [Fact]
    public async Task UndoAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetTrackedByIdAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((Auction?)null);

        var act = () => _service.UndoAsync(_contractorId, id);

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task UndoAsync_soft_undoes_and_releases_route_when_found()
    {
        var route = Route();
        var auction = Auction(route.Id);
        var segment = Segment();
        segment.Status = SegmentStatus.Routed;
        segment.RouteId = route.Id;
        segment.RouteSequence = 1;

        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);
        _segmentRepository.GetTrackedByRouteAsync(route.Id, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { segment });
        var bidder = Guid.NewGuid();
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = bidder, Status = BidStatus.Pending },
            new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = Guid.NewGuid(), Status = BidStatus.Withdrawn },
        });

        await _service.UndoAsync(_contractorId, auction.Id);

        // Only carriers with a live bid are told; the label is taken before the segment is released.
        await _notifier.Received(1).AuctionCancelledAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { bidder })),
            $"{segment.OriginAddress} → {segment.DestinationAddress}", Arg.Any<CancellationToken>());
        auction.Status.Should().Be(AuctionStatus.Undone);
        // Undo releases the route and its segments so they become available again.
        route.Status.Should().Be(RouteStatus.Planned);
        segment.Status.Should().Be(SegmentStatus.Available);
        segment.RouteId.Should().BeNull();
        segment.RouteSequence.Should().BeNull();
        await _auctionRepository.DidNotReceive().DeleteAsync(Arg.Any<Auction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UndoAsync_throws_when_auction_not_open()
    {
        var auction = Auction(status: AuctionStatus.Closed);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);

        var act = () => _service.UndoAsync(_contractorId, auction.Id);

        await act.Should().ThrowAsync<AuctionNotOpenException>();
    }

    // =====================================================================
    // GetBidRankingAsync
    // =====================================================================

    [Fact]
    public async Task GetBidRankingAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetDetailAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((AuctionWithRoute?)null);

        var act = () => _service.GetBidRankingAsync(_contractorId, id);

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task GetBidRankingAsync_ranks_bids_with_savings()
    {
        var auction = Auction();
        var route = Route(ceiling: 10000m);
        var seg = Segment(products: TestData.Product());
        var detail = new AuctionWithRoute(auction, route, new[] { seg });

        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);

        var carrier = TestData.Carrier();
        var vehicle = TestData.Vehicle(carrierId: carrier.Id);
        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            CarrierId = carrier.Id,
            VehicleId = vehicle.Id,
            NetFreightValue = 7000m,
            TollValue = 500m,
            TotalValue = 7500m,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = BidStatus.Pending,
        };

        _bidRepository.GetRankedBidsAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new RankedBid(bid, carrier, vehicle) });

        var result = await _service.GetBidRankingAsync(_contractorId, auction.Id);

        result.AuctionId.Should().Be(auction.Id);
        result.Bids.Should().ContainSingle();
        var row = result.Bids[0];
        row.Rank.Should().Be(1);
        row.BidId.Should().Be(bid.Id);
        row.Financials.SavingsValue.Should().Be(2500m); // 10000 - 7500
        row.Financials.SavingsPercentage.Should().BeApproximately(25d, 0.001);
        result.Route.SegmentCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBidRankingAsync_zero_ceiling_yields_zero_savings_percentage()
    {
        var auction = Auction();
        var route = Route(ceiling: 0m);
        var seg = Segment(products: TestData.Product());
        var detail = new AuctionWithRoute(auction, route, new[] { seg });

        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);

        var carrier = TestData.Carrier();
        var vehicle = TestData.Vehicle(carrierId: carrier.Id);
        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            CarrierId = carrier.Id,
            VehicleId = vehicle.Id,
            NetFreightValue = 100m,
            TollValue = 0m,
            TotalValue = 100m,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = BidStatus.Pending,
        };

        _bidRepository.GetRankedBidsAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new RankedBid(bid, carrier, vehicle) });

        var result = await _service.GetBidRankingAsync(_contractorId, auction.Id);

        result.Bids[0].Financials.SavingsPercentage.Should().Be(0d);
    }

    [Fact]
    public async Task GetBidRankingAsync_uses_company_name_when_trade_name_missing()
    {
        var auction = Auction();
        var detail = new AuctionWithRoute(auction, Route(), new[] { Segment(products: TestData.Product()) });
        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);

        var carrier = TestData.Carrier(companyName: "Razao Social SA");
        carrier.TradeName = null;
        var vehicle = TestData.Vehicle(carrierId: carrier.Id);
        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            CarrierId = carrier.Id,
            VehicleId = vehicle.Id,
            NetFreightValue = 100m,
            TollValue = 0m,
            TotalValue = 100m,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = BidStatus.Pending,
        };
        _bidRepository.GetRankedBidsAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new RankedBid(bid, carrier, vehicle) });

        var result = await _service.GetBidRankingAsync(_contractorId, auction.Id);

        result.Bids[0].Carrier.TradeName.Should().Be("Razao Social SA");
    }

    // =====================================================================
    // AwardAsync
    // =====================================================================

    [Fact]
    public async Task AwardAsync_auction_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetTrackedByIdAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((Auction?)null);

        var act = () => _service.AwardAsync(_contractorId, id, new AwardAuctionRequestDto(Guid.NewGuid()));

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task AwardAsync_not_open_throws()
    {
        var auction = Auction(status: AuctionStatus.Closed);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);

        var act = () => _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(Guid.NewGuid()));

        await act.Should().ThrowAsync<AuctionNotOpenException>();
    }

    [Fact]
    public async Task AwardAsync_winning_bid_not_found_throws()
    {
        var auction = Auction();
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Bid>());

        var act = () => _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(Guid.NewGuid()));

        await act.Should().ThrowAsync<BidNotFoundException>();
    }

    [Fact]
    public async Task AwardAsync_route_missing_throws_AuctionNotFound()
    {
        var route = Route();
        var auction = Auction(routeId: route.Id);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);

        var bid = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = _carrierId, VehicleId = Guid.NewGuid(), TotalValue = 100m };
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { bid });
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns((ConsolidatedRoute?)null);

        var act = () => _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(bid.Id));

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task AwardAsync_happy_path_elects_winner_closes_auction_and_creates_trip()
    {
        var route = Route();
        var auction = Auction(routeId: route.Id);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);

        var winner = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = _carrierId, VehicleId = Guid.NewGuid(), TotalValue = 500m, Status = BidStatus.Pending };
        var loser = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = Guid.NewGuid(), VehicleId = Guid.NewGuid(), TotalValue = 900m, Status = BidStatus.Pending };
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { winner, loser });

        var detail = new AuctionWithRoute(auction, route, new[] { Segment(products: TestData.Product()) });
        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(winner.Id));

        result.WinningBidId.Should().Be(winner.Id);
        result.AuctionStatus.Should().Be(AuctionStatus.Closed.ToWire());
        result.RouteStatus.Should().Be(RouteStatus.AwaitingPickup.ToWire());

        winner.Status.Should().Be(BidStatus.Winner);
        loser.Status.Should().Be(BidStatus.Losing);
        auction.Status.Should().Be(AuctionStatus.Closed);
        route.Status.Should().Be(RouteStatus.AwaitingPickup);

        await _tripRepository.Received(1).AddAsync(
            Arg.Is<Trip>(t => t.AuctionId == auction.Id && t.BidId == winner.Id && t.AgreedValue == 500m),
            Arg.Any<CancellationToken>());
        await _notifier.Received(1).AuctionAwardedAsync(result.TripId, _carrierId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { loser.CarrierId })), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardAsync_auction_with_an_existing_winner_is_rejected()
    {
        var route = Route();
        var auction = Auction(routeId: route.Id);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);
        var previousWinner = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = _carrierId, VehicleId = Guid.NewGuid(), TotalValue = 500m, Status = BidStatus.Winner };
        var other = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = Guid.NewGuid(), VehicleId = Guid.NewGuid(), TotalValue = 600m, Status = BidStatus.Pending };
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { previousWinner, other });

        var act = () => _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(other.Id));

        await act.Should().ThrowAsync<AuctionNotOpenException>();
        await _tripRepository.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(RouteStatus.AwaitingPickup)]
    [InlineData(RouteStatus.InTransit)]
    [InlineData(RouteStatus.Completed)]
    public async Task AwardAsync_route_already_past_the_auction_is_rejected(RouteStatus routeStatus)
    {
        var route = Route();
        route.Status = routeStatus;
        var auction = Auction(routeId: route.Id);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);
        var bid = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = _carrierId, VehicleId = Guid.NewGuid(), TotalValue = 500m, Status = BidStatus.Pending };
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { bid });

        var act = () => _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(bid.Id));

        await act.Should().ThrowAsync<AuctionNotOpenException>();
        route.Status.Should().Be(routeStatus);
    }

    [Fact]
    public async Task AwardAsync_vehicle_already_booked_in_overlapping_window_is_rejected_without_changes()
    {
        var pickup = DateTimeOffset.UtcNow.AddDays(1);
        var delivery = DateTimeOffset.UtcNow.AddDays(2);
        var route = Route();
        var auction = Auction(routeId: route.Id);
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);

        var vehicleId = Guid.NewGuid();
        var winner = new Bid { Id = Guid.NewGuid(), AuctionId = auction.Id, CarrierId = _carrierId, VehicleId = vehicleId, TotalValue = 500m, Status = BidStatus.Pending };
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { winner });

        var detail = new AuctionWithRoute(auction, route, new[] { Segment(pickup: pickup, delivery: delivery, products: TestData.Product()) });
        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(detail);

        // The same vehicle won another auction after this bid was placed.
        _tripRepository.GetActiveWindowsForVehicleAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(new[] { new TripScheduleWindow(pickup.AddHours(3), delivery.AddHours(3)) });

        var act = () => _service.AwardAsync(_contractorId, auction.Id, new AwardAuctionRequestDto(winner.Id));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.Overbooked);
        await _vehicleRepository.Received(1).LockForScheduleAsync(vehicleId, Arg.Any<CancellationToken>());
        winner.Status.Should().Be(BidStatus.Pending);
        auction.Status.Should().Be(AuctionStatus.Open);
        route.Status.Should().NotBe(RouteStatus.AwaitingPickup);
        await _tripRepository.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // AutoAwardAsync
    // =====================================================================

    private (ConsolidatedRoute Route, Auction Auction, RouteSegment Segment) ExpiredAutomaticAuction(DateTimeOffset? pickup = null)
    {
        var route = Route();
        var auction = Auction(routeId: route.Id);
        auction.AutomaticAward = true;
        auction.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var segment = Segment(contractorId: _contractorId, status: SegmentStatus.Routed, routeId: route.Id,
            pickup: pickup ?? DateTimeOffset.UtcNow.AddDays(1), products: TestData.Product());
        _auctionRepository.GetTrackedByIdAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(auction);
        _routeRepository.GetTrackedByIdAsync(route.Id, Arg.Any<CancellationToken>()).Returns(route);
        _segmentRepository.GetTrackedByRouteAsync(route.Id, Arg.Any<CancellationToken>()).Returns(new List<RouteSegment> { segment });
        _auctionRepository.GetDetailAsync(auction.Id, _contractorId, Arg.Any<CancellationToken>())
            .Returns(new AuctionWithRoute(auction, route, new[] { segment }));
        return (route, auction, segment);
    }

    private Bid NewBid(Guid auctionId, decimal total, DateTimeOffset submittedAt, Guid? vehicleId = null) => new()
    {
        Id = Guid.NewGuid(), AuctionId = auctionId, CarrierId = Guid.NewGuid(), VehicleId = vehicleId ?? Guid.NewGuid(),
        TotalValue = total, SubmittedAt = submittedAt, Status = BidStatus.Pending,
    };

    [Fact]
    public async Task AutoAwardAsync_lowest_total_wins_and_earliest_breaks_a_tie()
    {
        var (_, auction, _) = ExpiredAutomaticAuction();
        var now = DateTimeOffset.UtcNow;
        var higher = NewBid(auction.Id, 900m, now.AddHours(-5));
        var lateTie = NewBid(auction.Id, 700m, now.AddHours(-1));
        var earlyTie = NewBid(auction.Id, 700m, now.AddHours(-2));
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { higher, lateTie, earlyTie });

        var outcome = await _service.AutoAwardAsync(_contractorId, auction.Id, now);

        outcome.Should().Be(AutoAwardOutcome.Awarded);
        earlyTie.Status.Should().Be(BidStatus.Winner);
        auction.Status.Should().Be(AuctionStatus.Closed);
        await _notifier.Received(1).AuctionAutoAwardedAsync(_contractorId, Arg.Any<Guid>(), Arg.Any<string>(), 700m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoAwardAsync_skips_a_bid_whose_vehicle_is_no_longer_free()
    {
        var (_, auction, segment) = ExpiredAutomaticAuction();
        var now = DateTimeOffset.UtcNow;
        var busyVehicle = Guid.NewGuid();
        var cheapest = NewBid(auction.Id, 500m, now.AddHours(-3), busyVehicle);
        var next = NewBid(auction.Id, 600m, now.AddHours(-2));
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { cheapest, next });
        _tripRepository.GetActiveWindowsForVehicleAsync(busyVehicle, Arg.Any<CancellationToken>())
            .Returns(new[] { new TripScheduleWindow(segment.PickupDeadline, segment.DeliveryDeadline) });

        var outcome = await _service.AutoAwardAsync(_contractorId, auction.Id, now);

        outcome.Should().Be(AutoAwardOutcome.Awarded);
        next.Status.Should().Be(BidStatus.Winner);
        cheapest.Status.Should().Be(BidStatus.Losing);
    }

    [Fact]
    public async Task AutoAwardAsync_without_bids_closes_and_releases_the_segments()
    {
        var (route, auction, segment) = ExpiredAutomaticAuction();
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<Bid>());

        var outcome = await _service.AutoAwardAsync(_contractorId, auction.Id, DateTimeOffset.UtcNow);

        outcome.Should().Be(AutoAwardOutcome.ClosedWithoutWinner);
        auction.Status.Should().Be(AuctionStatus.Closed);
        route.Status.Should().Be(RouteStatus.Planned);
        segment.Status.Should().Be(SegmentStatus.Available);
        segment.RouteId.Should().BeNull();
        await _tripRepository.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).AuctionEndedWithoutWinnerAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<string>(), "sem lances", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoAwardAsync_after_the_first_pickup_deadline_does_not_create_a_late_trip()
    {
        var (_, auction, segment) = ExpiredAutomaticAuction(pickup: DateTimeOffset.UtcNow.AddDays(-3));
        var bid = NewBid(auction.Id, 500m, DateTimeOffset.UtcNow.AddDays(-4));
        _bidRepository.GetTrackedByAuctionAsync(auction.Id, Arg.Any<CancellationToken>()).Returns(new[] { bid });

        var outcome = await _service.AutoAwardAsync(_contractorId, auction.Id, DateTimeOffset.UtcNow);

        outcome.Should().Be(AutoAwardOutcome.ClosedWithoutWinner);
        bid.Status.Should().Be(BidStatus.Losing);
        segment.Status.Should().Be(SegmentStatus.Available);
        await _tripRepository.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoAwardAsync_route_that_already_has_a_trip_only_closes_the_auction()
    {
        var (route, auction, segment) = ExpiredAutomaticAuction();
        route.Status = RouteStatus.Completed;
        segment.Status = SegmentStatus.Completed;

        var outcome = await _service.AutoAwardAsync(_contractorId, auction.Id, DateTimeOffset.UtcNow);

        outcome.Should().Be(AutoAwardOutcome.ClosedStale);
        auction.Status.Should().Be(AuctionStatus.Closed);
        route.Status.Should().Be(RouteStatus.Completed);
        segment.Status.Should().Be(SegmentStatus.Completed);
    }

    [Fact]
    public async Task AutoAwardAsync_ignores_manual_or_unexpired_auctions()
    {
        var (_, auction, _) = ExpiredAutomaticAuction();
        auction.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

        (await _service.AutoAwardAsync(_contractorId, auction.Id, DateTimeOffset.UtcNow)).Should().Be(AutoAwardOutcome.Skipped);

        auction.ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);
        auction.AutomaticAward = false;
        (await _service.AutoAwardAsync(_contractorId, auction.Id, DateTimeOffset.UtcNow)).Should().Be(AutoAwardOutcome.Skipped);
        auction.Status.Should().Be(AuctionStatus.Open);
    }

    // =====================================================================
    // GetCarrierAnalysisAsync
    // =====================================================================

    [Fact]
    public async Task GetCarrierAnalysisAsync_not_available_throws_OfferNotFound()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetAvailableForCarrierAsync(id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((CarrierAuctionWithRoute?)null);

        var act = () => _service.GetCarrierAnalysisAsync(_carrierId, id);

        await act.Should().ThrowAsync<OfferNotFoundException>();
    }

    [Fact]
    public async Task GetCarrierAnalysisAsync_returns_analysis_with_fleet_and_mybid()
    {
        var route = Route(ceiling: 6000m);
        var auction = Auction(routeId: route.Id);
        var seg = Segment(products: TestData.Product());
        var detail = new CarrierAuctionWithRoute(auction, route, new[] { seg }, "Contratante");

        _auctionRepository.GetAvailableForCarrierAsync(auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(detail);
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AuctionBidMetrics(auction.Id, 5000m, 3) });
        _bidRepository.GetBestBidWithCarrierAsync(auction.Id, Arg.Any<CancellationToken>())
            .Returns(new BestBidWithCarrier(5000m, "Lider"));

        var vehicle = TestData.Vehicle(carrierId: _carrierId);
        _vehicleRepository.GetAllAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(new[] { vehicle });

        var myBid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            CarrierId = _carrierId,
            VehicleId = vehicle.Id,
            NetFreightValue = 4000m,
            TollValue = 100m,
            TotalValue = 4100m,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = BidStatus.Pending,
        };
        _bidRepository.GetByCarrierAndAuctionAsync(_carrierId, auction.Id, Arg.Any<CancellationToken>()).Returns(myBid);

        var result = await _service.GetCarrierAnalysisAsync(_carrierId, auction.Id);

        result.AuctionId.Should().Be(auction.Id);
        result.Competition.ActiveBids.Should().Be(3);
        result.Competition.BestLeaderOffer.Should().Be(5000m);
        result.Competition.AuctionCeiling.Should().Be(6000m);
        result.Competition.EstimatedAnttFloor.Should().Be(4800m);
        result.CarrierAvailableFleet.Should().ContainSingle().Which.VehicleId.Should().Be(vehicle.Id);
        result.MyBid.Should().NotBeNull();
        result.MyBid!.BidId.Should().Be(myBid.Id);
    }

    [Fact]
    public async Task GetCarrierAnalysisAsync_null_mybid_maps_to_null()
    {
        var route = Route();
        var auction = Auction(routeId: route.Id);
        var detail = new CarrierAuctionWithRoute(auction, route, new[] { Segment(products: TestData.Product()) }, "C");

        _auctionRepository.GetAvailableForCarrierAsync(auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(detail);
        _bidRepository.GetMetricsForAuctionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuctionBidMetrics>());
        _bidRepository.GetBestBidWithCarrierAsync(auction.Id, Arg.Any<CancellationToken>()).Returns((BestBidWithCarrier?)null);
        _vehicleRepository.GetAllAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Vehicle>());
        _bidRepository.GetByCarrierAndAuctionAsync(_carrierId, auction.Id, Arg.Any<CancellationToken>()).Returns((Bid?)null);

        var result = await _service.GetCarrierAnalysisAsync(_carrierId, auction.Id);

        result.MyBid.Should().BeNull();
        result.Competition.BestLeaderOffer.Should().BeNull();
    }

    // =====================================================================
    // PlaceBidAsync
    // =====================================================================

    private CarrierAuctionWithRoute AvailableOffer(
        decimal ceiling = 5000m,
        double weightKg = 1500,
        double volumeM3 = 10,
        Product[]? products = null)
    {
        var route = Route(ceiling: ceiling, weightKg: weightKg, volumeM3: volumeM3);
        var auction = Auction(routeId: route.Id);
        var seg = Segment(products: products ?? new[] { TestData.Product() });
        return new CarrierAuctionWithRoute(auction, route, new[] { seg }, "Contratante");
    }

    [Fact]
    public async Task PlaceBidAsync_offer_not_found_throws()
    {
        var id = Guid.NewGuid();
        _auctionRepository.GetAvailableForCarrierAsync(id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((CarrierAuctionWithRoute?)null);

        var act = () => _service.PlaceBidAsync(_carrierId, id, new PlaceBidRequestDto(100m, Guid.NewGuid()));

        await act.Should().ThrowAsync<OfferNotFoundException>();
    }

    [Fact]
    public async Task PlaceBidAsync_non_positive_value_rejected_InvalidValue()
    {
        var offer = AvailableOffer();
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(0m, Guid.NewGuid()));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.InvalidValue);
    }

    [Fact]
    public async Task PlaceBidAsync_above_ceiling_rejected()
    {
        var offer = AvailableOffer(ceiling: 1000m);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(2000m, Guid.NewGuid()));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.AboveCeiling);
    }

    [Fact]
    public async Task PlaceBidAsync_freight_within_ceiling_but_total_with_toll_above_is_rejected()
    {
        // Segment toll defaults to 50: freight 1000 + toll 50 = 1050 > ceiling 1000.
        var offer = AvailableOffer(ceiling: 1000m);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(1000m, Guid.NewGuid()));

        var ex = (await act.Should().ThrowAsync<BidRejectedException>()).Which;
        ex.Code.Should().Be(BidRejectionCode.AboveCeiling);
        ex.Message.Should().Contain("R$").And.Contain("1.050,00");
        await _bidRepository.DidNotReceive().AddAsync(Arg.Any<Bid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_vehicle_not_found_throws_KeyNotFound()
    {
        var offer = AvailableOffer();
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        _vehicleRepository.GetByIdAsync(Arg.Any<Guid>(), _carrierId, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(100m, Guid.NewGuid()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PlaceBidAsync_overweight_rejected_and_logs_blocked_attempt()
    {
        var offer = AvailableOffer(weightKg: 50000);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        var vehicle = TestData.Vehicle(carrierId: _carrierId); // capacity 20000kg
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(100m, vehicleId));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.Overweight);
        await _blockedBidAttemptRepository.Received(1).AddAsync(
            Arg.Is<BlockedBidAttempt>(b => b.Reason == BlockedReason.Weight), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_overvolume_rejected_and_logs_blocked_attempt()
    {
        var offer = AvailableOffer(weightKg: 100, volumeM3: 500);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        var vehicle = TestData.Vehicle(carrierId: _carrierId); // capacity 80 m3
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(100m, vehicleId));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.Overvolume);
        await _blockedBidAttemptRepository.Received(1).AddAsync(
            Arg.Is<BlockedBidAttempt>(b => b.Reason == BlockedReason.Volume), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_equipment_mismatch_rejected_and_logs_blocked_attempt()
    {
        var frozen = TestData.Product();
        frozen.TransportEnvironment = TransportEnvironment.Frozen; // requires Congelado refrigeration
        var offer = AvailableOffer(weightKg: 100, volumeM3: 5, products: new[] { frozen });
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        var vehicle = TestData.Vehicle(carrierId: _carrierId); // RefrigerationLevel.Nenhuma
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var act = () => _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(100m, vehicleId));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.EquipmentMismatch);
        await _blockedBidAttemptRepository.Received(1).AddAsync(
            Arg.Is<BlockedBidAttempt>(b => b.Reason == BlockedReason.Equipment), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_overbooking_rejected_and_logs_blocked_attempt()
    {
        var pickup = DateTimeOffset.UtcNow.AddDays(1);
        var delivery = DateTimeOffset.UtcNow.AddDays(2);
        var route = Route(ceiling: 5000m, weightKg: 100, volumeM3: 5);
        var auction = Auction(routeId: route.Id);
        var seg = Segment(pickup: pickup, delivery: delivery, products: TestData.Product());
        var offer = new CarrierAuctionWithRoute(auction, route, new[] { seg }, "C");

        _auctionRepository.GetAvailableForCarrierAsync(auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        var vehicle = TestData.Vehicle(carrierId: _carrierId);
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);

        _tripRepository.GetActiveWindowsForVehicleAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(new[] { new TripScheduleWindow(pickup.AddHours(-1), delivery.AddHours(1)) });

        var act = () => _service.PlaceBidAsync(_carrierId, auction.Id, new PlaceBidRequestDto(100m, vehicleId));

        (await act.Should().ThrowAsync<BidRejectedException>()).Which.Code.Should().Be(BidRejectionCode.Overbooked);
        await _blockedBidAttemptRepository.Received(1).AddAsync(
            Arg.Is<BlockedBidAttempt>(b => b.Reason == BlockedReason.Sla), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_happy_path_creates_new_bid()
    {
        var offer = AvailableOffer(ceiling: 5000m, weightKg: 100, volumeM3: 5);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        var vehicle = TestData.Vehicle(carrierId: _carrierId);
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);
        _tripRepository.GetActiveWindowsForVehicleAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TripScheduleWindow>());
        _bidRepository.GetTrackedByCarrierAndAuctionAsync(_carrierId, offer.Auction.Id, Arg.Any<CancellationToken>())
            .Returns((Bid?)null);

        var result = await _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(1000m, vehicleId));

        // toll = 50 (single segment default) -> total 1050
        result.NetFreightValue.Should().Be(1000m);
        result.TollValue.Should().Be(50m);
        result.TotalValue.Should().Be(1050m);
        result.Status.Should().Be(BidStatus.Pending.ToWire());
        await _bidRepository.Received(1).AddAsync(Arg.Any<Bid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_happy_path_updates_existing_bid()
    {
        var offer = AvailableOffer(ceiling: 5000m, weightKg: 100, volumeM3: 5);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        var vehicle = TestData.Vehicle(carrierId: _carrierId);
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(vehicle);
        _tripRepository.GetActiveWindowsForVehicleAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TripScheduleWindow>());

        var existing = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = offer.Auction.Id,
            CarrierId = _carrierId,
            VehicleId = Guid.NewGuid(),
            NetFreightValue = 500m,
            TollValue = 50m,
            TotalValue = 550m,
            SubmittedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Status = BidStatus.Losing,
        };
        _bidRepository.GetTrackedByCarrierAndAuctionAsync(_carrierId, offer.Auction.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(1200m, vehicleId));

        result.BidId.Should().Be(existing.Id);
        existing.NetFreightValue.Should().Be(1200m);
        existing.VehicleId.Should().Be(vehicleId);
        existing.Status.Should().Be(BidStatus.Pending);
        await _bidRepository.DidNotReceive().AddAsync(Arg.Any<Bid>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).BidPlacedAsync(offer.Auction.Id, offer.Route.ContractorId, _carrierId, Arg.Any<string>(),
            1250m, true, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1100, true)]   // 1100 + 50 toll = 1150 < leader's 1200: the leader is outbid
    [InlineData(1150, false)]  // 1150 + 50 = 1200 ties the leader: it keeps the lead
    public async Task PlaceBidAsync_notifies_the_contractor_and_the_carrier_that_lost_the_lead(decimal freight, bool outbid)
    {
        var offer = AvailableOffer(ceiling: 5000m, weightKg: 100, volumeM3: 5);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(TestData.Vehicle(carrierId: _carrierId));
        _tripRepository.GetActiveWindowsForVehicleAsync(vehicleId, Arg.Any<CancellationToken>()).Returns(Array.Empty<TripScheduleWindow>());
        _bidRepository.GetTrackedByCarrierAndAuctionAsync(_carrierId, offer.Auction.Id, Arg.Any<CancellationToken>()).Returns((Bid?)null);
        var leader = TestData.Carrier();
        var leaderBid = new Bid { Id = Guid.NewGuid(), AuctionId = offer.Auction.Id, CarrierId = leader.Id, TotalValue = 1200m, Status = BidStatus.Pending };
        _bidRepository.GetRankedBidsAsync(offer.Auction.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new RankedBid(leaderBid, leader, TestData.Vehicle(carrierId: leader.Id)) });

        await _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(freight, vehicleId));

        await _notifier.Received(1).BidPlacedAsync(offer.Auction.Id, offer.Route.ContractorId, _carrierId, Arg.Any<string>(),
            freight + 50m, false, outbid ? leader.Id : null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceBidAsync_rebid_by_the_leader_outbids_nobody()
    {
        var offer = AvailableOffer(ceiling: 5000m, weightKg: 100, volumeM3: 5);
        _auctionRepository.GetAvailableForCarrierAsync(offer.Auction.Id, _carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        var vehicleId = Guid.NewGuid();
        _vehicleRepository.GetByIdAsync(vehicleId, _carrierId, Arg.Any<CancellationToken>()).Returns(TestData.Vehicle(carrierId: _carrierId));
        _tripRepository.GetActiveWindowsForVehicleAsync(vehicleId, Arg.Any<CancellationToken>()).Returns(Array.Empty<TripScheduleWindow>());
        var own = new Bid { Id = Guid.NewGuid(), AuctionId = offer.Auction.Id, CarrierId = _carrierId, TotalValue = 1200m, Status = BidStatus.Pending };
        _bidRepository.GetTrackedByCarrierAndAuctionAsync(_carrierId, offer.Auction.Id, Arg.Any<CancellationToken>()).Returns(own);
        _bidRepository.GetRankedBidsAsync(offer.Auction.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new RankedBid(own, TestData.Carrier(_carrierId), TestData.Vehicle(carrierId: _carrierId)) });

        await _service.PlaceBidAsync(_carrierId, offer.Auction.Id, new PlaceBidRequestDto(900m, vehicleId));

        await _notifier.Received(1).BidPlacedAsync(offer.Auction.Id, offer.Route.ContractorId, _carrierId, Arg.Any<string>(),
            950m, true, null, Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // VehicleCompatibilityEvaluator (internal, visible to tests)
    // =====================================================================

    [Fact]
    public void Evaluate_refrigeration_mismatch_returns_rejection()
    {
        var vehicle = TestData.Vehicle(); // Nenhuma
        var requirement = new ConsolidatedVehicleRequirement("Baú / Sider", "Congelado", false, false);

        var result = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);

        result.Should().NotBeNull();
        result!.Code.Should().Be(BidRejectionCode.EquipmentMismatch);
        result.Message.Should().Contain("refrigeração");
    }

    [Fact]
    public void Evaluate_bodywork_mismatch_returns_rejection()
    {
        // Bau vehicle cannot carry a "Tanque" requirement.
        var vehicle = TestData.Vehicle();
        var requirement = new ConsolidatedVehicleRequirement("Tanque", "Nenhuma", false, false);

        var result = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);

        result.Should().NotBeNull();
        result!.Code.Should().Be(BidRejectionCode.EquipmentMismatch);
    }

    [Fact]
    public void Evaluate_mopp_required_but_missing_returns_rejection()
    {
        var vehicle = TestData.Vehicle(); // HasMopp false
        var requirement = new ConsolidatedVehicleRequirement("Baú / Sider", "Nenhuma", RequiresMopp: true, RequiresCargoFixing: false);

        var result = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);

        result.Should().NotBeNull();
        result!.Message.Should().Contain("MOPP");
    }

    [Fact]
    public void Evaluate_cargo_securing_required_but_missing_returns_rejection()
    {
        var vehicle = new Vehicle(
            transportadoraId: Guid.NewGuid(),
            plate: "ABC1D23",
            model: "Volvo FH",
            axleCount: 3,
            capacityWeight: 20000m,
            capacityVolume: 80m,
            bodyType: VehicleBodyType.Bau,
            refrigerationLevel: RefrigerationLevel.Nenhuma,
            hasMopp: false,
            hasCargoSecuring: false, // missing
            driver: "João",
            currentLocation: "Curitiba, PR");
        var requirement = new ConsolidatedVehicleRequirement("Baú / Sider", "Nenhuma", RequiresMopp: false, RequiresCargoFixing: true);

        var result = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);

        result.Should().NotBeNull();
        result!.Message.Should().Contain("fixação de carga");
    }

    [Fact]
    public void Evaluate_compatible_vehicle_returns_null()
    {
        var vehicle = TestData.Vehicle(); // Bau, Nenhuma, HasCargoSecuring true
        var requirement = new ConsolidatedVehicleRequirement("Baú / Sider", "Nenhuma", RequiresMopp: false, RequiresCargoFixing: false);

        var result = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);

        result.Should().BeNull();
    }

    [Fact]
    public void Evaluate_refrigerated_vehicle_satisfies_dry_bodywork_family()
    {
        var vehicle = new Vehicle(
            transportadoraId: Guid.NewGuid(),
            plate: "ABC1D23",
            model: "Volvo FH",
            axleCount: 3,
            capacityWeight: 20000m,
            capacityVolume: 80m,
            bodyType: VehicleBodyType.Frigorifico,
            refrigerationLevel: RefrigerationLevel.Congelado,
            hasMopp: true,
            hasCargoSecuring: true,
            driver: "João",
            currentLocation: "Curitiba, PR");
        var requirement = new ConsolidatedVehicleRequirement("Baú / Sider", "Congelado", RequiresMopp: true, RequiresCargoFixing: true);

        var result = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);

        result.Should().BeNull();
    }
}
