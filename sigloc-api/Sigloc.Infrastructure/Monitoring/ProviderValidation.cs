using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;

namespace Sigloc.Infrastructure.Monitoring;

internal static class ProviderValidation
{
    internal static bool IsCoordinateValid(double latitude, double longitude) =>
        double.IsFinite(latitude) && latitude is >= -90 and <= 90 &&
        double.IsFinite(longitude) && longitude is >= -180 and <= 180;

    internal static void ValidateCoordinates(IReadOnlyList<RouteCoordinate> coordinates)
    {
        if (coordinates is null || coordinates.Any(c => c is null || !IsCoordinateValid(c.Latitude, c.Longitude)))
            throw new TrackingUnavailableException();
    }

    internal static Uri Endpoint(string baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new TrackingUnavailableException();

        return new Uri(uri, path);
    }

    internal static CancellationTokenSource Timeout(MonitoringProviderSettings settings, CancellationToken cancellationToken)
    {
        if (settings.ProviderTimeoutSeconds <= 0 || settings.ProviderTimeoutSeconds > uint.MaxValue / 1000)
            throw new TrackingUnavailableException();

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(TimeSpan.FromSeconds(settings.ProviderTimeoutSeconds));
        return source;
    }
}
