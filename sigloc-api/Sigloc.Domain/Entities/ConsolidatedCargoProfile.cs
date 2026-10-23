using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Calculated (never stored) handling profile of everything a trip carries. Reuses the
/// "strongest wins" rule of <see cref="ConsolidatedVehicleRequirement"/> for refrigeration,
/// bodywork, MOPP and cargo fixing, and adds the tightest temperature window and packaging.
/// </summary>
public sealed record ConsolidatedCargoProfile(
    ConsolidatedVehicleRequirement Requirement,
    TransportEnvironment Environment,
    double? TempMin,
    double? TempMax,
    bool Palletized,
    IReadOnlyList<string> Packagings,
    IReadOnlyList<(string Name, int Quantity)> Products)
{
    public static ConsolidatedCargoProfile From(IEnumerable<ProductRouteSegment> items)
    {
        var loaded = items.Where(i => i.Product != null).ToList();
        var products = loaded.Select(i => i.Product!).DistinctBy(p => p.Id).ToList();

        // The window every product tolerates: the highest minimum and the lowest maximum.
        var minimums = products.Where(p => p.TempMin != null).Select(p => p.TempMin!.Value).ToList();
        var maximums = products.Where(p => p.TempMax != null).Select(p => p.TempMax!.Value).ToList();

        return new ConsolidatedCargoProfile(
            Requirement: ConsolidatedVehicleRequirement.From(products),
            Environment: products.Select(p => p.TransportEnvironment).DefaultIfEmpty(TransportEnvironment.Dry).Max(),
            TempMin: minimums.Count == 0 ? null : minimums.Max(),
            TempMax: maximums.Count == 0 ? null : maximums.Min(),
            Palletized: products.Any(p => p.PackagingType == PackagingType.Palletized),
            Packagings: products.Where(p => p.PackagingType != null).Select(p => p.PackagingType!.Value)
                .Distinct().Order().Select(p => p.ToWire()).ToList(),
            Products: loaded.GroupBy(i => i.ProductId)
                .Select(g => (g.First().Product!.Name, g.Sum(i => i.Quantity)))
                .OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList());
    }
}
