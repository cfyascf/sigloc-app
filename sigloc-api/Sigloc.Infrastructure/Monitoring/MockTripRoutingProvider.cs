using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;

namespace Sigloc.Infrastructure.Monitoring;

public sealed class MockTripRoutingProvider(IOptions<MonitoringProviderSettings> options) : ITripRoutingProvider
{
    private readonly MonitoringProviderSettings _settings = options.Value;

    public Task<TripRouteEstimate> CalculateAsync(IReadOnlyList<RouteCoordinate> coordinates, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProviderValidation.ValidateCoordinates(coordinates);
        if (!double.IsFinite(_settings.MockSpeedKmPerHour) || _settings.MockSpeedKmPerHour <= 0)
            throw new TrackingUnavailableException();

        double totalKm = 0;
        double nextStopSeconds = 0;
        for (var index = 0; index < coordinates.Count - 1; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var distance = DistanceKm(coordinates[index], coordinates[index + 1]);
            totalKm += distance;
            if (index == 0)
                nextStopSeconds = distance / _settings.MockSpeedKmPerHour * 3600;
        }
        if (!double.IsFinite(totalKm) || !double.IsFinite(nextStopSeconds))
            throw new TrackingUnavailableException();
        return Task.FromResult(new TripRouteEstimate(totalKm, nextStopSeconds));
    }

    private static double DistanceKm(RouteCoordinate from, RouteCoordinate to)
    {
        const double radians = Math.PI / 180;
        var latitudeDelta = (to.Latitude - from.Latitude) * radians;
        var longitudeDelta = (to.Longitude - from.Longitude) * radians;
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
            Math.Cos(from.Latitude * radians) * Math.Cos(to.Latitude * radians) *
            Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return 6371.0088 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
    }
}
