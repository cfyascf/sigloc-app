using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class RoutePreviewServiceTests
{
    private readonly IRouteSegmentRepository _repository = Substitute.For<IRouteSegmentRepository>();
    private readonly IRouteGeocodingService _geocoding = Substitute.For<IRouteGeocodingService>();
    private readonly RoutePreviewService _service;
    private readonly Guid _contractorId = Guid.NewGuid();

    public RoutePreviewServiceTests() => _service = new RoutePreviewService(_repository, _geocoding);

    private RouteSegment Segment(Guid id, Product product, int quantity, double distance = 100, decimal? budget = 500m)
        => new()
        {
            Id = id,
            ContractorId = _contractorId,
            OriginAddress = "O",
            DestinationAddress = "D",
            OriginCoordinate = "-49,-25",
            DestinationCoordinate = "-46,-23",
            DistanceKm = distance,
            EstimatedTimeHours = 2,
            BudgetCeiling = budget,
            EstimatedTollCost = 10m,
            PickupDeadline = DateTimeOffset.UtcNow,
            DeliveryDeadline = DateTimeOffset.UtcNow.AddDays(1),
            Status = SegmentStatus.Available,
            Items = new List<ProductRouteSegment>
            {
                new() { ProductId = product.Id, Quantity = quantity, Product = product }
            }
        };

    [Fact]
    public async Task PreviewAsync_aggregates_segments_into_response()
    {
        var product = TestData.Product(contractorId: _contractorId, weight: 10, volume: 2);
        var id = Guid.NewGuid();
        var segment = Segment(id, product, quantity: 3, distance: 100, budget: 500m);

        _repository
            .GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { segment });

        var result = await _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(new[] { id }));

        result.TotalDistanceKm.Should().Be(100);
        result.ConsolidatedCeiling.Should().Be(500m);
        result.EstimatedAnttFloor.Should().Be(500m);
        result.EstimatedToll.Should().Be(10m);
        result.CostPerKm.Should().Be(Math.Round(500m / 100m, 2));
        result.AggregatedTotals.TotalWeightKg.Should().Be(30);
        result.AggregatedTotals.TotalVolumeM3.Should().Be(6);
        result.ConsolidatedVehicleRequirement.Should().NotBeNull();
        await _repository.Received(1).GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewAsync_maps_consolidated_vehicle_requirement()
    {
        var product = TestData.Product(contractorId: _contractorId, category: ProductCategory.LiquidBulk);
        product.TransportEnvironment = TransportEnvironment.Frozen;
        product.Dangerous = true;
        product.Fragile = true;
        var id = Guid.NewGuid();

        _repository
            .GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { Segment(id, product, quantity: 1) });

        var result = await _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(new[] { id }));

        result.ConsolidatedVehicleRequirement.BaseBodyworkType.Should().Be("Tanque");
        result.ConsolidatedVehicleRequirement.MinRefrigerationLevel.Should().Be("Congelado");
        result.ConsolidatedVehicleRequirement.RequiresMopp.Should().BeTrue();
        result.ConsolidatedVehicleRequirement.RequiresCargoFixing.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewAsync_empty_segment_ids_throws_validation()
    {
        var act = () => _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(Array.Empty<Guid>()));

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "segmentIds");
    }

    [Fact]
    public async Task PreviewAsync_null_segment_ids_throws_validation()
    {
        var act = () => _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(null));

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().Contain(e => e.Field == "segmentIds");
    }

    [Fact]
    public async Task PreviewAsync_all_empty_guids_throws_validation()
    {
        var act = () => _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(new[] { Guid.Empty, Guid.Empty }));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PreviewAsync_missing_segment_propagates_not_found()
    {
        var present = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var product = TestData.Product(contractorId: _contractorId);

        _repository
            .GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { Segment(present, product, quantity: 1) });

        var act = () => _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(new[] { present, missing }));

        var ex = (await act.Should().ThrowAsync<RouteSegmentsNotFoundException>()).Which;
        ex.MissingIds.Should().Contain(missing);
    }

    [Fact]
    public async Task PreviewAsync_geocoding_leg_failure_propagates()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        _repository
            .GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { Segment(id1, product, 1), Segment(id2, product, 1) });

        _geocoding
            .ComputeLegAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<RouteLeg>(_ => throw new GeocodingException("Routing provider unavailable."));

        var act = () => _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(new[] { id1, id2 }));

        await act.Should().ThrowAsync<GeocodingException>();
    }

    [Fact]
    public async Task PreviewAsync_deduplicates_segment_ids()
    {
        var product = TestData.Product(contractorId: _contractorId);
        var id = Guid.NewGuid();

        _repository
            .GetByIdsAsync(_contractorId, Arg.Any<IReadOnlyCollection<Guid>>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<RouteSegment> { Segment(id, product, 1) });

        await _service.PreviewAsync(_contractorId, new RoutePreviewRequestDto(new[] { id, id }));

        await _repository.Received(1).GetByIdsAsync(
            _contractorId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(id)),
            false,
            Arg.Any<CancellationToken>());
    }
}
