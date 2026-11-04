using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

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

        EnsureSingleCargoClass(ordered);
        EnsureSingleCategory(ordered);
        EnsureSingleProductSegmentsAlone(ordered);
        EnsureCompatibleTemperatures(ordered);

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

    /// <summary>
    /// Dangerous, fragile and common goods can't share a route. Checked over every product, so an
    /// older segment that already mixes classes is caught too.
    /// </summary>
    private static void EnsureSingleCargoClass(IReadOnlyList<RouteSegment> segments)
    {
        var bySegment = segments
            .Select(s => (Segment: s, Classes: CargoClassification.ClassesOf(s.Items.Where(i => i.Product is not null).Select(i => i.Product!))))
            .ToList();
        var classes = bySegment.SelectMany(x => x.Classes).Distinct().OrderByDescending(c => c).ToList();
        if (classes.Count <= 1)
        {
            return;
        }

        // "carga perigosa (trechos 1a2b3c4d) e carga comum (trecho 5e6f7a8b)"
        var parts = classes.Select(cargoClass =>
        {
            var ids = bySegment.Where(x => x.Classes.Contains(cargoClass)).Select(x => ShortId(x.Segment.Id)).ToList();
            return $"{CargoClassification.Label(cargoClass)} ({(ids.Count == 1 ? "trecho" : "trechos")} {string.Join(", ", ids)})";
        });
        throw new ValidationException(
            [new ValidationError("segmentIds", $"Trechos com classes de carga diferentes não podem formar a mesma rota: {string.Join(" e ", parts)}.")],
            "Não foi possível consolidar a rota.");
    }

    /// <summary>
    /// Each product category needs its own body type, so a route carries a single category. Checked
    /// over every product, so an older segment that already mixes categories is caught too.
    /// </summary>
    private static void EnsureSingleCategory(IReadOnlyList<RouteSegment> segments)
    {
        var bySegment = segments
            .Select(s => (Segment: s, Categories: s.Items.Where(i => i.Product is not null).Select(i => i.Product!.Category).ToHashSet()))
            .ToList();
        var categories = bySegment.SelectMany(x => x.Categories).Distinct().Order().ToList();
        if (categories.Count <= 1)
        {
            return;
        }

        // "Geral (trechos 1a2b3c4d, 5e6f7a8b) e Granel Sólido (trecho 9c8d7e6f)"
        var parts = categories.Select(category =>
        {
            var ids = bySegment.Where(x => x.Categories.Contains(category)).Select(x => ShortId(x.Segment.Id)).ToList();
            return $"{category.ToLabel()} ({(ids.Count == 1 ? "trecho" : "trechos")} {string.Join(", ", ids)})";
        });
        throw new ValidationException(
            [new ValidationError("segmentIds", $"Trechos com categorias de carga diferentes não podem formar a mesma rota: {string.Join(" e ", parts)}.")],
            "Não foi possível consolidar a rota.");
    }

    /// <summary>
    /// Bulk and live cargo fill the whole vehicle with one product: such a segment is offered on its
    /// own, and must carry a single product (an older segment may still hold more).
    /// </summary>
    private static void EnsureSingleProductSegmentsAlone(IReadOnlyList<RouteSegment> segments)
    {
        foreach (var segment in segments)
        {
            var products = segment.Items.Where(i => i.Product is not null).Select(i => i.Product!).ToList();
            var single = products.FirstOrDefault(p => CargoCompatibility.IsSingleProduct(p.Category));
            if (single is null)
            {
                continue;
            }

            if (products.Select(p => p.Id).Distinct().Count() > 1)
            {
                throw new ValidationException(
                    [new ValidationError("segmentIds",
                        $"O trecho {ShortId(segment.Id)} leva mais de um produto {single.Category.ToLabel()}: edite o trecho e deixe um único produto antes de ofertá-lo.")],
                    "Não foi possível consolidar a rota.");
            }

            if (segments.Count > 1)
            {
                throw new ValidationException(
                    [new ValidationError("segmentIds",
                        $"O trecho {ShortId(segment.Id)} ({single.Category.ToLabel()}) deve ser ofertado sozinho: cargas a granel e carga viva "
                        + "ocupam o veículo inteiro e não se combinam com outros trechos.")],
                    "Não foi possível consolidar a rota.");
            }
        }
    }

    /// <summary>
    /// The route travels at a single temperature, so every product on it must tolerate it. Checked
    /// over every product, so an older segment that already mixes temperatures is caught too.
    /// </summary>
    private static void EnsureCompatibleTemperatures(IReadOnlyList<RouteSegment> segments)
    {
        var carried = segments
            .SelectMany(s => s.Items.Where(i => i.Product is not null).Select(i => (Segment: s, Product: i.Product!)))
            .ToList();
        if (TemperatureCompatibility.FindConflict(carried.Select(x => x.Product)) is not (Product warmest, Product coldest))
        {
            return;
        }

        string SegmentOf(Product product) => ShortId(carried.First(x => x.Product == product).Segment.Id);

        // "Arroz (ambiente, 0 °C a 35 °C) no trecho 1a2b3c4d e Sorvete (-25 °C a -18 °C) no trecho 5e6f7a8b"
        throw new ValidationException(
            [new ValidationError("segmentIds",
                $"Trechos com temperaturas incompatíveis não podem formar a mesma rota: {TemperatureCompatibility.Describe(warmest)} "
                + $"no trecho {SegmentOf(warmest)} e {TemperatureCompatibility.Describe(coldest)} no trecho {SegmentOf(coldest)}.")],
            "Não foi possível consolidar a rota.");
    }

    // Same short code the segment cards show (the random tail of the UUIDv7).
    private static string ShortId(Guid id) => id.ToString("N")[^8..];
}
