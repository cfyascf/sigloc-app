using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;

namespace Sigloc.Application.Services;

/// <summary>
/// Builds the chronological "Milking Run" travel plan for a consolidated route. The
/// matrix is not stored: it is derived on demand by expanding every segment into a
/// pickup and a delivery event, ordering them by their SLA deadlines and aggregating
/// consecutive events that share the same city and day into a single stop.
/// </summary>
internal static class TravelPlanBuilder
{
    private enum StopKind
    {
        Pickup,
        Delivery
    }

    private sealed record StopEvent(StopKind Kind, string CityState, DateTimeOffset Deadline);

    /// <summary>
    /// Expands the segments into ordered travel-plan stops, coalescing same-city and
    /// same-day events of the same kind. The delivery with the latest deadline is
    /// labelled "Entrega Final"; earlier deliveries are "Entrega Parcial".
    /// </summary>
    public static IReadOnlyList<TravelPlanStopDto> Build(IReadOnlyList<RouteSegment> segments)
    {
        var events = new List<StopEvent>(segments.Count * 2);
        foreach (var segment in segments)
        {
            events.Add(new StopEvent(StopKind.Pickup, segment.OriginAddress, segment.PickupDeadline));
            events.Add(new StopEvent(StopKind.Delivery, segment.DestinationAddress, segment.DeliveryDeadline));
        }

        if (events.Count == 0)
        {
            return Array.Empty<TravelPlanStopDto>();
        }

        // Chronological ordering by SLA; deliveries after pickups on ties for readability.
        var ordered = events
            .OrderBy(e => e.Deadline)
            .ThenBy(e => e.Kind == StopKind.Pickup ? 0 : 1)
            .ToList();

        var lastDeliveryDeadline = ordered
            .Where(e => e.Kind == StopKind.Delivery)
            .Max(e => e.Deadline);

        // Group consecutive events sharing kind + normalized city + calendar day.
        var groups = new List<(StopKind Kind, string CityState, DateTimeOffset Deadline, int Count)>();
        foreach (var ev in ordered)
        {
            if (groups.Count > 0)
            {
                var current = groups[^1];
                if (current.Kind == ev.Kind
                    && CityKey(current.CityState) == CityKey(ev.CityState)
                    && current.Deadline.Date == ev.Deadline.Date)
                {
                    // Aggregate: keep the earliest deadline of the group, bump the count.
                    groups[^1] = (current.Kind, current.CityState, current.Deadline, current.Count + 1);
                    continue;
                }
            }

            groups.Add((ev.Kind, ev.CityState, ev.Deadline, 1));
        }

        var stops = new List<TravelPlanStopDto>(groups.Count);
        var order = 1;
        foreach (var group in groups)
        {
            var actionType = group.Kind == StopKind.Pickup
                ? $"Coleta ({group.Count} {(group.Count == 1 ? "Trecho" : "Trechos")})"
                : group.Deadline == lastDeliveryDeadline ? "Entrega Final" : "Entrega Parcial";

            stops.Add(new TravelPlanStopDto(
                Order: order++,
                CityState: group.CityState,
                ActionType: actionType,
                Deadline: group.Deadline));
        }

        return stops;
    }

    /// <summary>
    /// Ordered list of distinct cities in the order they are first visited. Used to build
    /// the consolidated itinerary strings for the listing.
    /// </summary>
    public static IReadOnlyList<string> OrderedCities(IReadOnlyList<TravelPlanStopDto> stops)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cities = new List<string>();
        foreach (var stop in stops)
        {
            var key = CityKey(stop.CityState);
            if (seen.Add(key))
            {
                cities.Add(stop.CityState);
            }
        }

        return cities;
    }

    /// <summary>Normalized comparison key for a "City, UF" string (city portion, case/space-insensitive).</summary>
    private static string CityKey(string cityState)
    {
        var comma = cityState.IndexOf(',');
        var city = comma >= 0 ? cityState[..comma] : cityState;
        return city.Trim().ToLowerInvariant();
    }
}
