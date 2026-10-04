using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class RouteSegmentServiceTests
{
    private readonly IRouteSegmentRepository _repository = Substitute.For<IRouteSegmentRepository>();
    private readonly IRouteGeocodingService _geocoding = Substitute.For<IRouteGeocodingService>();
    private readonly RouteSegmentService _service;
    private readonly Guid _contractorId = Guid.NewGuid();

    public RouteSegmentServiceTests()
    {
        _service = new RouteSegmentService(_repository, _geocoding);
        _geocoding
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RouteGeometry("-49.1,-25.1", "-46.6,-23.5", 400, 6));
    }

    private static RouteSegmentRequestDto ValidDto(
        string? origin = "Curitiba, PR",
        string? destination = "São Paulo, SP",
        decimal? budget = 1000m,
        decimal? toll = 50m,
        DateTimeOffset? pickup = null,
        DateTimeOffset? delivery = null,
        IReadOnlyList<RouteSegmentItemRequestDto>? items = null)
    {
        pickup ??= new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        delivery ??= new DateTimeOffset(2030, 1, 2, 8, 0, 0, TimeSpan.Zero);
        return new RouteSegmentRequestDto(origin, destination, budget, toll, pickup, delivery, items);
    }

    private static RouteSegmentItemRequestDto Item(Guid? productId, int? quantity = 2)
        => new(productId, quantity);

    // ---------------- CreateAsync ----------------

    [Fact]
    public async Task CreateAsync_valid_persists_and_maps_response()
    {
        var product = TestData.Product(contractorId: _contractorId, weight: 10, volume: 2);
        _repository
            .GetProductsByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        var dto = ValidDto(items: new[] { Item(product.Id, 3) });

        var result = await _service.CreateAsync(_contractorId, dto);

        result.Origin.Should().Be("Curitiba, PR");
        result.Destination.Should().Be("São Paulo, SP");
        result.ContractorId.Should().Be(_contractorId);
        result.DistanceKm.Should().Be(400);
        result.Status.Should().Be("AVAILABLE");
        result.Items.Should().ContainSingle();
        result.Items[0].WeightSubtotal.Should().Be(30);
        result.Items[0].VolumeSubtotal.Should().Be(6);
        result.CalculatedTotals.TotalWeightKg.Should().Be(30);
        result.CalculatedTotals.TotalVolumeM3.Should().Be(6);
        await _repository.Received(1).AddAsync(Arg.Any<RouteSegment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_trims_origin_and_destination()
    {
        var product = TestData.Product(contractorId: _contractorId);
        _repository
            .GetProductsByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        var dto = ValidDto(origin: "  Curitiba  ", destination: "  Sampa  ", items: new[] { Item(product.Id) });

        var result = await _service.CreateAsync(_contractorId, dto);

        result.Origin.Should().Be("Curitiba");
        result.Destination.Should().Be("Sampa");
    }

    [Fact]
    public async Task CreateAsync_blank_origin_and_destination_throws_validation()
    {
        var dto = ValidDto(origin: "   ", destination: "", items: new[] { Item(Guid.NewGuid()) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "origin");
        ex.Errors.Should().Contain(e => e.Field == "destination");
    }

    [Fact]
    public async Task CreateAsync_missing_deadlines_throws_validation()
    {
        var dto = new RouteSegmentRequestDto("A", "B", 1m, 1m, null, null, new[] { Item(Guid.NewGuid()) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "pickupDeadline");
        ex.Errors.Should().Contain(e => e.Field == "deliveryDeadline");
    }

    [Fact]
    public async Task CreateAsync_delivery_not_after_pickup_throws_validation()
    {
        var when = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var dto = ValidDto(pickup: when, delivery: when, items: new[] { Item(Guid.NewGuid()) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "deliveryDeadline" && e.Reason.Contains("later"));
    }

    [Fact]
    public async Task CreateAsync_negative_toll_throws_validation()
    {
        var dto = ValidDto(toll: -1m, items: new[] { Item(Guid.NewGuid()) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "estimatedTollCost");
    }

    [Fact]
    public async Task CreateAsync_null_toll_throws_validation()
    {
        var dto = ValidDto(toll: null, items: new[] { Item(Guid.NewGuid()) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "estimatedTollCost");
    }

    [Fact]
    public async Task CreateAsync_negative_budget_throws_validation()
    {
        var dto = ValidDto(budget: -1m, items: new[] { Item(Guid.NewGuid()) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "budgetCeiling");
    }

    [Fact]
    public async Task CreateAsync_no_items_throws_validation()
    {
        var dto = ValidDto(items: Array.Empty<RouteSegmentItemRequestDto>());

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "items");
    }

    [Fact]
    public async Task CreateAsync_null_items_throws_validation()
    {
        var dto = ValidDto(items: null);

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "items");
    }

    [Fact]
    public async Task CreateAsync_item_missing_product_id_throws_validation()
    {
        var dto = ValidDto(items: new[] { Item(Guid.Empty) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "items[0].productId");
    }

    [Fact]
    public async Task CreateAsync_item_non_positive_quantity_throws_validation()
    {
        var dto = ValidDto(items: new[] { Item(Guid.NewGuid(), 0) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "items[0].quantity");
    }

    [Fact]
    public async Task CreateAsync_unknown_product_throws_validation()
    {
        var unknownId = Guid.NewGuid();
        _repository
            .GetProductsByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var dto = ValidDto(items: new[] { Item(unknownId) });

        var act = () => _service.CreateAsync(_contractorId, dto);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "items" && e.Reason.Contains(unknownId.ToString()));
        await _repository.DidNotReceive().AddAsync(Arg.Any<RouteSegment>(), Arg.Any<CancellationToken>());
    }

    // ---------------- GetByIdAsync ----------------

    [Fact]
    public async Task GetByIdAsync_returns_mapped_segment()
    {
        var product = TestData.Product(contractorId: _contractorId, weight: 5, volume: 1);
        var segment = BuildSegment(product, quantity: 4);
        _repository
            .GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>())
            .Returns(segment);

        var result = await _service.GetByIdAsync(_contractorId, segment.Id);

        result.Id.Should().Be(segment.Id);
        result.CalculatedTotals.TotalWeightKg.Should().Be(20);
        result.Items.Should().ContainSingle(i => i.ProductId == product.Id);
    }

    [Fact]
    public async Task GetByIdAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((RouteSegment?)null);

        var act = () => _service.GetByIdAsync(_contractorId, id);

        await act.Should().ThrowAsync<RouteSegmentNotFoundException>();
    }

    // ---------------- SearchAsync ----------------

    [Fact]
    public async Task SearchAsync_normalizes_paging_and_maps()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        _repository
            .SearchAsync(_contractorId, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<RouteSegment> { segment }, 1));

        var result = await _service.SearchAsync(_contractorId, new RouteSegmentQueryDto(null, null, null, Page: 0, PageSize: 0));

        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalItems.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchAsync_caps_page_size_at_100()
    {
        _repository
            .SearchAsync(_contractorId, null, null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<RouteSegment>(), 0));

        var result = await _service.SearchAsync(_contractorId, new RouteSegmentQueryDto(null, null, null, Page: 1, PageSize: 500));

        result.PageSize.Should().Be(100);
        result.TotalPages.Should().Be(0);
        await _repository.Received(1).SearchAsync(_contractorId, null, null, null, 1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_parses_status_filter()
    {
        _repository
            .SearchAsync(_contractorId, null, null, SegmentStatus.InTransit, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<RouteSegment>(), 0));

        await _service.SearchAsync(_contractorId, new RouteSegmentQueryDto(null, null, "IN_TRANSIT"));

        await _repository.Received(1).SearchAsync(
            _contractorId, null, null, SegmentStatus.InTransit, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_invalid_status_throws_validation()
    {
        var act = () => _service.SearchAsync(_contractorId, new RouteSegmentQueryDto(null, null, "NOPE"));

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "status");
    }

    // ---------------- UpdateAsync ----------------

    [Fact]
    public async Task UpdateAsync_itinerary_unchanged_does_not_re_resolve()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        _repository.GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(segment);
        _repository
            .GetProductsByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        var dto = ValidDto(origin: segment.OriginAddress, destination: segment.DestinationAddress, items: new[] { Item(product.Id, 2) });

        var result = await _service.UpdateAsync(_contractorId, segment.Id, dto);

        result.Id.Should().Be(segment.Id);
        await _geocoding.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(segment, Arg.Any<IReadOnlyCollection<ProductRouteSegment>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_itinerary_changed_re_resolves_geometry()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        _repository.GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(segment);
        _repository
            .GetProductsByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        var dto = ValidDto(origin: "New Origin", destination: "New Dest", items: new[] { Item(product.Id, 1) });

        var result = await _service.UpdateAsync(_contractorId, segment.Id, dto);

        result.Origin.Should().Be("New Origin");
        result.DistanceKm.Should().Be(400);
        await _geocoding.Received(1).ResolveAsync("New Origin", "New Dest", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((RouteSegment?)null);

        var act = () => _service.UpdateAsync(_contractorId, id, ValidDto(items: new[] { Item(Guid.NewGuid()) }));

        await act.Should().ThrowAsync<RouteSegmentNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_not_editable_throws()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        segment.Status = SegmentStatus.Routed;
        _repository.GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(segment);

        var act = () => _service.UpdateAsync(_contractorId, segment.Id, ValidDto(items: new[] { Item(product.Id) }));

        await act.Should().ThrowAsync<RouteSegmentNotEditableException>();
    }

    [Fact]
    public async Task UpdateAsync_invalid_dto_throws_validation()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        _repository.GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(segment);

        var act = () => _service.UpdateAsync(_contractorId, segment.Id, ValidDto(origin: "", items: new[] { Item(product.Id) }));

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------------- DeleteAsync ----------------

    [Fact]
    public async Task DeleteAsync_available_deletes()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        _repository.GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(segment);

        await _service.DeleteAsync(_contractorId, segment.Id);

        await _repository.Received(1).DeleteAsync(segment, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_not_found_throws()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, _contractorId, Arg.Any<CancellationToken>()).Returns((RouteSegment?)null);

        var act = () => _service.DeleteAsync(_contractorId, id);

        await act.Should().ThrowAsync<RouteSegmentNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_not_editable_throws()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var segment = BuildSegment(product, quantity: 1);
        segment.Status = SegmentStatus.Completed;
        _repository.GetByIdAsync(segment.Id, _contractorId, Arg.Any<CancellationToken>()).Returns(segment);

        var act = () => _service.DeleteAsync(_contractorId, segment.Id);

        await act.Should().ThrowAsync<RouteSegmentNotEditableException>();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<RouteSegment>(), Arg.Any<CancellationToken>());
    }

    private RouteSegment BuildSegment(Product product, int quantity)
        => new()
        {
            Id = Guid.NewGuid(),
            ContractorId = _contractorId,
            OriginAddress = "Curitiba, PR",
            DestinationAddress = "São Paulo, SP",
            OriginCoordinate = "-49.1,-25.1",
            DestinationCoordinate = "-46.6,-23.5",
            DistanceKm = 400,
            EstimatedTimeHours = 6,
            BudgetCeiling = 1000m,
            EstimatedTollCost = 50m,
            PickupDeadline = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero),
            DeliveryDeadline = new DateTimeOffset(2030, 1, 2, 8, 0, 0, TimeSpan.Zero),
            Status = SegmentStatus.Available,
            Items = new List<ProductRouteSegment>
            {
                new() { ProductId = product.Id, Quantity = quantity, Product = product }
            }
        };
}

public class RouteAggregatorTests
{
    private readonly IRouteGeocodingService _geocoding = Substitute.For<IRouteGeocodingService>();

    private RouteSegment Segment(
        Guid id,
        double distance = 100,
        double time = 2,
        decimal? budget = 500m,
        decimal toll = 20m,
        Product? product = null,
        int quantity = 1)
    {
        var items = new List<ProductRouteSegment>();
        if (product is not null)
        {
            items.Add(new ProductRouteSegment { ProductId = product.Id, Quantity = quantity, Product = product });
        }

        return new RouteSegment
        {
            Id = id,
            ContractorId = Guid.NewGuid(),
            OriginAddress = "O",
            DestinationAddress = "D",
            OriginCoordinate = "-49,-25",
            DestinationCoordinate = "-46,-23",
            DistanceKm = distance,
            EstimatedTimeHours = time,
            BudgetCeiling = budget,
            EstimatedTollCost = toll,
            PickupDeadline = DateTimeOffset.UtcNow,
            DeliveryDeadline = DateTimeOffset.UtcNow.AddDays(1),
            Status = SegmentStatus.Available,
            Items = items
        };
    }

    [Fact]
    public async Task AggregateAsync_sums_totals_and_inter_segment_legs()
    {
        var p1 = TestData.Product(weight: 10, volume: 2);
        var p2 = TestData.Product(weight: 5, volume: 1);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var s1 = Segment(id1, distance: 100, time: 2, budget: 500m, toll: 20m, product: p1, quantity: 2);
        var s2 = Segment(id2, distance: 200, time: 3, budget: 300m, toll: 10m, product: p2, quantity: 4);

        _geocoding
            .ComputeLegAsync(s1.DestinationCoordinate, s2.OriginCoordinate, Arg.Any<CancellationToken>())
            .Returns(new RouteLeg(50, 1));

        var result = await RouteAggregator.AggregateAsync(
            new[] { id1, id2 }, new[] { s1, s2 }, _geocoding, CancellationToken.None);

        result.TotalDistanceKm.Should().Be(350);
        result.EstimatedTimeHours.Should().Be(6);
        result.ConsolidatedCeiling.Should().Be(800m);
        result.EstimatedAnttFloor.Should().Be(800m);
        result.EstimatedToll.Should().Be(30m);
        result.TotalWeightKg.Should().Be(10 * 2 + 5 * 4);
        result.TotalVolumeM3.Should().Be(2 * 2 + 1 * 4);
        result.CostPerKm.Should().Be(Math.Round(800m / 350m, 2));
        await _geocoding.Received(1).ComputeLegAsync(s1.DestinationCoordinate, s2.OriginCoordinate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AggregateAsync_missing_segment_throws()
    {
        var present = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var s = Segment(present);

        var act = () => RouteAggregator.AggregateAsync(
            new[] { present, missing }, new[] { s }, _geocoding, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<RouteSegmentsNotFoundException>()).Which;
        ex.MissingIds.Should().Contain(missing);
    }

    [Fact]
    public async Task AggregateAsync_zero_distance_yields_zero_cost_per_km()
    {
        var id = Guid.NewGuid();
        var s = Segment(id, distance: 0, time: 0, budget: 100m, toll: 0m);

        var result = await RouteAggregator.AggregateAsync(
            new[] { id }, new[] { s }, _geocoding, CancellationToken.None);

        result.TotalDistanceKm.Should().Be(0);
        result.CostPerKm.Should().Be(0m);
        await _geocoding.DidNotReceive().ComputeLegAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AggregateAsync_single_segment_has_no_legs()
    {
        var id = Guid.NewGuid();
        var s = Segment(id, distance: 123.456, time: 1.111, budget: 10m);

        var result = await RouteAggregator.AggregateAsync(
            new[] { id }, new[] { s }, _geocoding, CancellationToken.None);

        result.TotalDistanceKm.Should().Be(123.46);
        result.EstimatedTimeHours.Should().Be(1.11);
        await _geocoding.DidNotReceive().ComputeLegAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
