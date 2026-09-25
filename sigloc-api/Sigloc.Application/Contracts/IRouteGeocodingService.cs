namespace Sigloc.Application.Contracts;

/// <summary>Geographic route computed for an origin/destination pair.</summary>
public sealed record RouteGeometry(
    string OriginCoordinate,
    string DestinationCoordinate,
    double DistanceKm,
    double EstimatedTimeHours);

/// <summary>Distance and duration of a driving leg between two known coordinates.</summary>
public sealed record RouteLeg(
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

    /// <summary>
    /// Computes the outbound driving distance/duration between two already-known
    /// coordinates (each formatted as "longitude,latitude"). Used to measure the leg
    /// between consecutive segments (destination of one → origin of the next).
    /// </summary>
    Task<RouteLeg> ComputeLegAsync(string fromCoordinate, string toCoordinate, CancellationToken cancellationToken = default);
}
