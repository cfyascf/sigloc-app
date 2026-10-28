using Sigloc.Domain.Entities;

namespace Sigloc.Application.Services;

/// <summary>
/// Checks a manual stop confirmation for signs that the vehicle could not have been there. The
/// confirmation is always accepted; a non-null result is logged as a "PARADA_SUSPEITA" event and
/// sent to the contractor.
/// </summary>
public static class TripStopPlausibility
{
    // Straight-line distance between stops at this speed gives a rough driving time; arriving in
    // less than half of it is implausible for a truck.
    internal const double AssumedAverageSpeedKmh = 60;
    internal const double MinimumShareOfExpectedTime = 0.5;

    // Stops are geocoded at city level, so the tolerance is a few kilometres, not the 200 m
    // geofence used for exact arrivals. Only a recent ping counts.
    internal const double MaxDistanceFromStopKm = 5;
    internal static readonly TimeSpan RecentPing = TimeSpan.FromMinutes(30);

    /// <summary>Reason the confirmation of <paramref name="stop"/> looks implausible, or null.</summary>
    public static string? Check(Trip trip, TripStop stop, TripMonitoring? snapshot, DateTimeOffset now)
    {
        var reasons = new List<string>();

        var previous = trip.Stops
            .Where(s => s.IsCompleted && s.CompletedAt != null && s.Sequence < stop.Sequence)
            .OrderByDescending(s => s.Sequence)
            .FirstOrDefault();
        if (previous != null)
        {
            var km = TripMonitoringCalculator.DistanceMeters(previous.Latitude, previous.Longitude, stop.Latitude, stop.Longitude) / 1000;
            var expected = TimeSpan.FromHours(km / AssumedAverageSpeedKmh);
            var elapsed = now - previous.CompletedAt!.Value;
            if (km >= 1 && elapsed < expected * MinimumShareOfExpectedTime)
            {
                reasons.Add($"confirmada {FormatDuration(elapsed)} após a parada {previous.Sequence} ({previous.City}), "
                    + $"para um trajeto estimado de {FormatDuration(expected)}");
            }
        }

        if (snapshot is { Latitude: double lat, Longitude: double lon } && now - snapshot.LastPingAt <= RecentPing)
        {
            var km = TripMonitoringCalculator.DistanceMeters(lat, lon, stop.Latitude, stop.Longitude) / 1000;
            if (km > MaxDistanceFromStopKm)
            {
                reasons.Add($"o último sinal de GPS estava a {km:0} km de {stop.City}");
            }
        }

        return reasons.Count == 0 ? null : string.Join("; ", reasons);
    }

    private static string FormatDuration(TimeSpan value) =>
        value.TotalMinutes < 60 ? $"{Math.Max(0, (int)value.TotalMinutes)} min" : $"{(int)value.TotalHours}h{value.Minutes:00}";
}
