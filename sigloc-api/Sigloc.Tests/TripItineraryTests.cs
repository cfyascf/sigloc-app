using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Tests;

public class TripItineraryTests
{
    [Fact]
    public void Segment_order_is_preserved_over_deadline_order_and_revisits_remain_distinct()
    {
        var first = TripMonitoringServiceTests.Segment();
        var second = TripMonitoringServiceTests.Segment();
        second.RouteSequence = 2; second.PickupDeadline = first.PickupDeadline.AddDays(-1);
        var stops = TripItineraryBuilder.Build(Guid.NewGuid(), new[] { second, first });
        Assert.Equal(4, stops.Count);
        Assert.Equal(first.Id, stops[0].Actions[0].SegmentId);
        Assert.Equal(second.Id, stops[2].Actions[0].SegmentId);
    }

    [Fact]
    public void Legacy_order_uses_deadline_then_id_without_changing_distance()
    {
        var first = TripMonitoringServiceTests.Segment(); first.RouteSequence = null;
        var second = TripMonitoringServiceTests.Segment(); second.RouteSequence = null;
        second.PickupDeadline = first.PickupDeadline.AddDays(-1); second.DistanceKm = 123;
        var stops = TripItineraryBuilder.Build(Guid.NewGuid(), new[] { first, second });
        Assert.Equal(second.Id, stops[0].Actions[0].SegmentId);
        Assert.Equal(123, second.DistanceKm);
    }

    [Fact]
    public void Only_adjacent_matching_physical_visits_merge_with_distinct_action_deadlines()
    {
        var first = TripMonitoringServiceTests.Segment();
        var second = TripMonitoringServiceTests.Segment(); second.RouteSequence = 2;
        second.OriginAddress = "destination, bb"; second.OriginCoordinate = first.DestinationCoordinate;
        second.DestinationAddress = "Final, CC"; second.DestinationCoordinate = "2,2";
        second.PickupDeadline = first.DeliveryDeadline.AddHours(1);
        var stops = TripItineraryBuilder.Build(Guid.NewGuid(), new[] { first, second });
        Assert.Equal(3, stops.Count);
        Assert.Equal(2, stops[1].Actions.Count);
        Assert.Equal(2, stops[1].Actions.Select(a => a.Deadline).Distinct().Count());
    }

    [Fact]
    public void Same_city_different_states_or_coordinates_do_not_merge()
    {
        var first = TripMonitoringServiceTests.Segment();
        var second = TripMonitoringServiceTests.Segment(); second.RouteSequence = 2;
        second.OriginAddress = "Destination, CC"; second.OriginCoordinate = first.DestinationCoordinate;
        var stops = TripItineraryBuilder.Build(Guid.NewGuid(), new[] { first, second });
        Assert.Equal(4, stops.Count);
        second.OriginAddress = first.DestinationAddress; second.OriginCoordinate = "1.1,1";
        Assert.Equal(4, TripItineraryBuilder.Build(Guid.NewGuid(), new[] { first, second }).Count);
    }

    [Fact]
    public void Legacy_completion_does_not_invent_actual_timestamps()
    {
        var segment = TripMonitoringServiceTests.Segment(); segment.Status = SegmentStatus.InTransit;
        var stops = TripItineraryBuilder.Build(Guid.NewGuid(), new[] { segment });
        Assert.True(stops[0].IsCompleted); Assert.Null(stops[0].CompletedAt);
        Assert.Equal("LEGACY_STATE", stops[0].CompletionSource); Assert.False(stops[1].IsCompleted);
    }

    [Fact]
    public void Products_are_frozen_in_individual_stop_actions()
    {
        var segment = TripMonitoringServiceTests.Segment();
        var product = new Product { Id = Guid.NewGuid(), Sku = "SKU", Name = "Original" };
        segment.Items.Add(new ProductRouteSegment { ProductId = product.Id, Product = product });
        var stops = TripItineraryBuilder.Build(Guid.NewGuid(), new[] { segment });
        product.Name = "Changed";
        Assert.All(stops, s => Assert.Equal("Original", s.Actions[0].ProductName));
    }

    [Theory]
    [InlineData(100, 200, false, 0, 0)] [InlineData(100, 50, false, 50, 50)]
    [InlineData(100, 0, false, 100, 100)] [InlineData(0, 100, false, 0, 0)]
    [InlineData(0, 0, true, 0, 100)] [InlineData(100, 0, true, 100, 100)]
    public void Progress_is_bounded(double total, double remaining, bool finished, double traveled, double progress)
    {
        Assert.Equal((traveled, progress), TripMonitoringCalculator.Progress(total, remaining, finished));
    }

    [Fact]
    public void Geofence_boundary_uses_meter_distance_and_utilization_does_not_clamp()
    {
        var degrees = 200d / 6371000 * 180 / Math.PI;
        Assert.InRange(TripMonitoringCalculator.DistanceMeters(0, 0, degrees, 0), 199.999999, 200.000001);
        Assert.Equal(150, TripMonitoringCalculator.Utilization(30, 20));
        Assert.Null(TripMonitoringCalculator.Utilization(30, 0));
    }
}
