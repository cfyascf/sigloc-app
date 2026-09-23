using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

/// <summary>
/// Shared logic for loading the route segments referenced by a request and computing
/// their aggregated totals (including the inter-segment legs, via OpenRouteService).
/// Used by both the route preview and the auction creation flow so their numbers
/// always agree, per the spec: "the snapshot created by POST /api/leiloes must match
/// what POST /api/rotas/preview showed".
/// </summary>
public class RouteSegmentAggregator
{
    private readonly IRouteSegmentRepository _repository;
    private readonly IRouteGeocodingService _geocodingService;

    public RouteSegmentAggregator(IRouteSegmentRepository repository, IRouteGeocodingService geocodingService)
    {
        _repository = repository;
        _geocodingService = geocodingService;
    }

    /// <summary>
    /// Loads the segments referenced by <paramref name="segmentIds"/>, preserving the
    /// caller's order (it determines the direction of every inter-segment leg), and
    /// computes the aggregated totals for the whole route.
    /// </summary>
    public async Task<(IReadOnlyList<RouteSegment> SegmentsInOrder, RouteAggregationResult Aggregation)> LoadAndAggregateAsync(
        Guid contractorId,
        IReadOnlyList<Guid> segmentIds,
        bool asTracking,
        CancellationToken cancellationToken)
    {
        var loaded = await _repository.GetByIdsAsync(contractorId, segmentIds, asTracking, cancellationToken);
        var byId = loaded.ToDictionary(s => s.Id);

        var missing = segmentIds.Where(id => !byId.ContainsKey(id)).Distinct().ToList();
        if (missing.Count > 0)
        {
            throw new RouteSegmentsNotFoundException(missing);
        }

        var segmentsInOrder = segmentIds.Select(id => byId[id]).ToList();

        var legs = new (double DistanceKm, double DurationHours)[Math.Max(segmentsInOrder.Count - 1, 0)];
        var legTasks = new Task<RouteLeg>[legs.Length];

        for (var i = 0; i < legs.Length; i++)
        {
            legTasks[i] = _geocodingService.ComputeLegAsync(
                segmentsInOrder[i].DestinationCoordinate,
                segmentsInOrder[i + 1].OriginCoordinate,
                cancellationToken);
        }

        await Task.WhenAll(legTasks);

        for (var i = 0; i < legs.Length; i++)
        {
            legs[i] = (legTasks[i].Result.DistanceKm, legTasks[i].Result.DurationHours);
        }

        var aggregation = RouteAggregation.Calculate(segmentsInOrder, legs);

        return (segmentsInOrder, aggregation);
    }
}
