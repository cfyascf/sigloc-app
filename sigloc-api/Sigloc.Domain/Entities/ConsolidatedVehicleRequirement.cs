using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Calculated (never stored) vehicle requirement consolidated across every product
/// carried on a route segment. Applies the "strongest wins" inheritance rule: the
/// most restrictive requirement among all products is adopted for the whole trip.
/// </summary>
public sealed record ConsolidatedVehicleRequirement(
    string BaseBodyworkType,
    string MinRefrigerationLevel,
    bool RequiresMopp,
    bool RequiresCargoFixing)
{
    /// <summary>
    /// Consolidates the vehicle requirement from all products carried on a segment.
    /// Refrigeration escalates Frozen &gt; Chilled &gt; Dry; bodywork adopts the most
    /// sensitive one; MOPP and cargo fixing become required if any single product
    /// demands them.
    /// </summary>
    public static ConsolidatedVehicleRequirement From(IEnumerable<Product> products)
    {
        var environment = TransportEnvironment.Dry;
        var category = ProductCategory.General;
        var hasCategory = false;
        var requiresMopp = false;
        var requiresCargoFixing = false;

        foreach (var product in products)
        {
            if (product.TransportEnvironment > environment)
            {
                environment = product.TransportEnvironment;
            }

            if (!hasCategory || IsMoreSensitiveBodywork(product.Category, category))
            {
                category = product.Category;
                hasCategory = true;
            }

            requiresMopp |= product.Dangerous;
            requiresCargoFixing |= product.Fragile;
        }

        return new ConsolidatedVehicleRequirement(
            BaseBodyworkType: ResolveBodywork(category),
            MinRefrigerationLevel: ResolveRefrigeration(environment),
            RequiresMopp: requiresMopp,
            RequiresCargoFixing: requiresCargoFixing);
    }

    private static bool IsMoreSensitiveBodywork(ProductCategory candidate, ProductCategory current)
        => BodyworkSensitivity(candidate) > BodyworkSensitivity(current);

    private static int BodyworkSensitivity(ProductCategory category) => category switch
    {
        ProductCategory.LiquidBulk => 2,
        ProductCategory.SolidBulk => 1,
        _ => 0
    };

    private static string ResolveBodywork(ProductCategory category) => category switch
    {
        ProductCategory.General => "Baú / Sider",
        ProductCategory.SolidBulk => "Graneleiro / Basculante",
        ProductCategory.LiquidBulk => "Tanque",
        _ => "Baú / Sider"
    };

    private static string ResolveRefrigeration(TransportEnvironment environment) => environment switch
    {
        TransportEnvironment.Dry => "Nenhuma",
        TransportEnvironment.Chilled => "Resfriado",
        TransportEnvironment.Frozen => "Congelado",
        _ => "Nenhuma"
    };
}
