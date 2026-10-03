using System.Globalization;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
namespace Sigloc.Application.Services;

public static class TripItineraryBuilder
{
    public static List<TripStop> Build(Guid tripId, IReadOnlyCollection<RouteSegment> segments)
    {
        var stops = new List<TripStop>();
        // Legacy order cannot be reconstructed; never rewrite the route distance snapshot.
        var ordered = segments.All(s => s.RouteSequence.HasValue)
            ? segments.OrderBy(s => s.RouteSequence).ThenBy(s => s.Id)
            : segments.OrderBy(s => s.PickupDeadline).ThenBy(s => s.Id);
        foreach (var segment in ordered)
        {
            Add(segment, StopActionKind.Pickup, segment.OriginAddress, segment.OriginCoordinate, segment.PickupDeadline);
            Add(segment, StopActionKind.Delivery, segment.DestinationAddress, segment.DestinationCoordinate, segment.DeliveryDeadline);
        }
        return stops;

        void Add(RouteSegment segment, StopActionKind kind, string address, string coordinate, DateTimeOffset deadline)
        {
            var parts = coordinate.Split(',');
            if (parts.Length != 2 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.IsFinite(lat) || !double.IsFinite(lon) || Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
                throw new TrackingUnavailableException();
            var location = address.Split(',', StringSplitOptions.TrimEntries);
            var city = location[0];
            var state = location.Length > 1 ? location[^1] : string.Empty;
            var stop = stops.LastOrDefault();
            if (stop is null || stop.Latitude != lat || stop.Longitude != lon
                || !string.Equals(stop.City, city, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(stop.State, state, StringComparison.OrdinalIgnoreCase))
            {
                stop = new TripStop { Id = Guid.NewGuid(), TripId = tripId, Sequence = stops.Count + 1,
                    Address = address, City = city, State = state, Latitude = lat, Longitude = lon };
                stops.Add(stop);
            }
            var complete = segment.Status == SegmentStatus.Completed
                || (kind == StopActionKind.Pickup && segment.Status == SegmentStatus.InTransit);
            var items = segment.Items.Count == 0 ? new ProductRouteSegment?[] { null } : segment.Items.Cast<ProductRouteSegment?>();
            foreach (var item in items)
                stop.Actions.Add(new TripStopAction { Id = Guid.NewGuid(), TripStopId = stop.Id, SegmentId = segment.Id,
                    ProductId = item?.ProductId, ProductName = item?.Product?.Name, Kind = kind, Deadline = deadline,
                    IsCompleted = complete, CompletionSource = complete ? "LEGACY_STATE" : null });
            stop.IsCompleted = stop.Actions.All(a => a.IsCompleted);
            stop.CompletionSource = stop.IsCompleted ? "LEGACY_STATE" : null;
        }
    }
}
