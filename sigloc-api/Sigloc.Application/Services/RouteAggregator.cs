using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;

namespace Sigloc.Application.Services;

/// <summary>
/// Computes the real-time consolidation math for a set of route segments: physical
/// totals, financial figures, the most restrictive vehicle requirement, and the total
/// distance/time including the driving legs between consecutive segments.
/// </summary>
internal static class RouteAggregator
{
    internal sealed record Result(
        double TotalDistanceKm,
        double EstimatedTimeHours,
        decimal ConsolidatedCeiling,
        decimal EstimatedAnttFloor,
        decimal EstimatedToll,
        decimal CostPerKm,
        double TotalWeightKg,
        double TotalVolumeM3,
        ConsolidatedVehicleRequirement VehicleRequirement);

    /// <summary>
    /// Validates and aggregates the requested segments. <paramref name="orderedIds"/>
    /// defines the visiting order used to measure inter-segment legs.
    /// </summary>
    public static async Task<Result> AggregateAsync(
        IReadOnlyList<Guid> orderedIds,
        IReadOnlyList<RouteSegment> segments,
        IRouteGeocodingService geocodingService,
        CancellationToken cancellationToken)
    {
        var byId = segments.ToDictionary(s => s.Id);

        var missing = orderedIds.Where(id => !byId.ContainsKey(id)).Distinct().ToList();
        if (missing.Count > 0)
        {
            throw new RouteSegmentsNotFoundException(missing);
        }

        var ordered = orderedIds.Select(id => byId[id]).ToList();

        double totalDistance = 0;
        double totalTime = 0;
        decimal consolidatedCeiling = 0;
        decimal estimatedToll = 0;
        double totalWeight = 0;
        double totalVolume = 0;
        var carriedProducts = new List<Product>();

        foreach (var segment in ordered)
        {
            totalDistance += segment.DistanceKm;
            totalTime += segment.EstimatedTimeHours;
            consolidatedCeiling += segment.BudgetCeiling ?? 0m;
            estimatedToll += segment.EstimatedTollCost;

            foreach (var item in segment.Items)
            {
                if (item.Product is null)
                {
                    continue;
                }

                totalWeight += item.Product.DefaultWeight * item.Quantity;
                totalVolume += item.Product.DefaultVolume * item.Quantity;
                carriedProducts.Add(item.Product);
            }
        }

        // Inter-segment legs: destination of one segment → origin of the next.
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var leg = await geocodingService.ComputeLegAsync(
                ordered[i].DestinationCoordinate,
                ordered[i + 1].OriginCoordinate,
                cancellationToken);

            totalDistance += leg.DistanceKm;
            totalTime += leg.EstimatedTimeHours;
        }

        totalDistance = Math.Round(totalDistance, 2);
        totalTime = Math.Round(totalTime, 2);

        // The consolidated ceiling is used as the base for the ANTT floor estimate.
        var anttFloor = consolidatedCeiling;
        var costPerKm = totalDistance > 0
            ? Math.Round(consolidatedCeiling / (decimal)totalDistance, 2)
            : 0m;

        var requirement = ConsolidatedVehicleRequirement.From(carriedProducts);

        return new Result(
            TotalDistanceKm: totalDistance,
            EstimatedTimeHours: totalTime,
            ConsolidatedCeiling: consolidatedCeiling,
            EstimatedAnttFloor: anttFloor,
            EstimatedToll: estimatedToll,
            CostPerKm: costPerKm,
            TotalWeightKg: totalWeight,
            TotalVolumeM3: totalVolume,
            VehicleRequirement: requirement);
    }
}
