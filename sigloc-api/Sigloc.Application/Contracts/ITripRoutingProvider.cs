namespace Sigloc.Application.Contracts;

public record RouteCoordinate(double Latitude, double Longitude);

public record TripRouteEstimate(double RemainingDistanceKm, double NextStopDurationSeconds);

public interface ITripRoutingProvider
{
    Task<TripRouteEstimate> CalculateAsync(IReadOnlyList<RouteCoordinate> coordinates, CancellationToken cancellationToken = default);
}
