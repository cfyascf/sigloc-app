namespace Sigloc.Domain.Entities;

/// <summary>
/// Aggregated totals for a group of route segments (Trechos) consolidated in order,
/// including the inter-segment legs (the distance/time between the destination of one
/// segment and the origin of the next). Never stored as-is; recomputed on demand by
/// both the route preview and the auction creation flow, so the two always agree.
/// </summary>
public sealed record RouteAggregationResult(
    double TotalDistanceKm,
    double TotalTimeHours,
    decimal ConsolidatedBudgetCeiling,
    decimal EstimatedTollCost,
    double TotalWeightKg,
    double TotalVolumeM3,
    ConsolidatedVehicleRequirement VehicleRequirement);

public static class RouteAggregation
{
    /// <summary>
    /// Computes the aggregated totals for <paramref name="segmentsInOrder"/>. Segments
    /// must be ordered exactly as the caller intends to travel them (the order in which
    /// the ids were submitted), and <paramref name="interSegmentLegs"/> must contain one
    /// entry per consecutive pair (segmentsInOrder.Count - 1 entries): the leg from the
    /// destination of segment i to the origin of segment i + 1. Each segment's Items
    /// must have their Product navigation loaded.
    /// </summary>
    public static RouteAggregationResult Calculate(
        IReadOnlyList<RouteSegment> segmentsInOrder,
        IReadOnlyList<(double DistanceKm, double DurationHours)> interSegmentLegs)
    {
        var totalDistance = segmentsInOrder.Sum(s => s.DistanceKm) + interSegmentLegs.Sum(l => l.DistanceKm);
        var totalTime = segmentsInOrder.Sum(s => s.EstimatedTimeHours) + interSegmentLegs.Sum(l => l.DurationHours);
        var budgetCeiling = segmentsInOrder.Sum(s => s.BudgetCeiling ?? 0m);
        var tollCost = segmentsInOrder.Sum(s => s.EstimatedTollCost);

        double totalWeight = 0;
        double totalVolume = 0;
        var carriedProducts = new List<Product>();

        foreach (var segment in segmentsInOrder)
        {
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

        var requirement = ConsolidatedVehicleRequirement.From(carriedProducts);

        return new RouteAggregationResult(
            TotalDistanceKm: Math.Round(totalDistance, 2),
            TotalTimeHours: Math.Round(totalTime, 2),
            ConsolidatedBudgetCeiling: budgetCeiling,
            EstimatedTollCost: tollCost,
            TotalWeightKg: totalWeight,
            TotalVolumeM3: totalVolume,
            VehicleRequirement: requirement);
    }
}
