using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Calculated (never stored) vehicle requirement consolidated across every product
/// carried on a route segment. Applies the "strongest wins" inheritance rule: the
/// most restrictive requirement among all products is adopted for the whole trip.
/// </summary>
public sealed record ConsolidatedVehicleRequirement(
    IReadOnlyList<VehicleBodyType> AcceptedBodyTypes,
    string MinRefrigerationLevel,
    bool RequiresMopp,
    bool RequiresCargoFixing,
    bool RequiresCivCipp = false)
{
    /// <summary>Accepted body types as display text ("Baú, Sider ou Grade Baixa").</summary>
    public string BaseBodyworkType => CargoCompatibility.Describe(AcceptedBodyTypes);

    /// <summary>
    /// Consolidates the vehicle requirement from all products carried on a segment.
    /// Refrigeration escalates Frozen &gt; Chilled &gt; Dry; the accepted body types are the
    /// ones every product accepts at that refrigeration (empty when the load mixes categories
    /// no single body carries); MOPP and cargo fixing become required if any single product
    /// demands them.
    /// </summary>
    public static ConsolidatedVehicleRequirement From(IEnumerable<Product> products)
    {
        var list = products.ToList();
        var environment = list.Count == 0 ? TransportEnvironment.Dry : list.Max(p => p.TransportEnvironment);
        var fragile = list.Any(p => p.Fragile);

        IReadOnlyList<VehicleBodyType> accepted = list.Count == 0
            ? CargoCompatibility.AcceptedBodies(ProductCategory.General, environment, fragile)
            : list.Select(p => p.Category).Distinct()
                .Select(category => CargoCompatibility.AcceptedBodies(category, environment, fragile).AsEnumerable())
                .Aggregate((acc, next) => acc.Intersect(next))
                .ToList();

        return new ConsolidatedVehicleRequirement(
            AcceptedBodyTypes: accepted,
            MinRefrigerationLevel: CargoCompatibility.RequiredRefrigeration(environment).ToString(),
            RequiresMopp: list.Any(p => p.Dangerous),
            RequiresCargoFixing: fragile,
            RequiresCivCipp: list.Any(p => p.Dangerous && CargoCompatibility.IsBulk(p.Category)));
    }
}
