namespace Sigloc.Application.Contracts;

/// <summary>Geographic route computed for an origin/destination pair.</summary>
public sealed record RouteGeometry(
    string OriginCoordinate,
    string DestinationCoordinate,
    double DistanceKm,
    double EstimatedTimeHours);

/// <summary>
/// Resolves textual addresses into coordinates and computes the driving distance and
/// duration between them (backed by an external routing provider such as OpenRouteService).
/// </summary>
public interface IRouteGeocodingService
{
    /// <summary>
    /// Geocodes both addresses and computes the outbound distance/duration from origin
    /// to destination. All external calls are performed asynchronously.
    /// </summary>
    Task<RouteGeometry> ResolveAsync(string originAddress, string destinationAddress, CancellationToken cancellationToken = default);
}
