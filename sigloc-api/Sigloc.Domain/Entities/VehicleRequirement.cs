using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Calculated (never stored) description of the vehicle characteristics required to
/// transport a product. Derived purely from the product's own attributes.
/// </summary>
public sealed record VehicleRequirement(
    IReadOnlyList<VehicleBodyType> AcceptedBodyTypes,
    string MinRefrigerationLevel,
    bool RequiresMopp,
    bool RequiresCargoFixing)
{
    /// <summary>Accepted body types as display text ("Baú, Sider ou Grade Baixa").</summary>
    public string BaseBodyworkType => CargoCompatibility.Describe(AcceptedBodyTypes);

    public static VehicleRequirement From(Product product) => new(
        AcceptedBodyTypes: CargoCompatibility.AcceptedBodies(product.Category, product.TransportEnvironment, product.Fragile),
        MinRefrigerationLevel: CargoCompatibility.RequiredRefrigeration(product.TransportEnvironment).ToString(),
        RequiresMopp: product.Dangerous,
        RequiresCargoFixing: product.Fragile);
}
