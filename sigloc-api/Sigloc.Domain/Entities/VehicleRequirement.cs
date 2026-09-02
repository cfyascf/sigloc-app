using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Calculated (never stored) description of the vehicle characteristics required to
/// transport a product. Derived purely from the product's own attributes.
/// </summary>
public sealed record VehicleRequirement(
    string BaseBodyworkType,
    string MinRefrigerationLevel,
    bool RequiresMopp,
    bool RequiresCargoFixing)
{
    public static VehicleRequirement From(Product product)
    {
        var bodywork = product.Category switch
        {
            ProductCategory.General => "Baú / Sider",
            ProductCategory.SolidBulk => "Graneleiro / Basculante",
            ProductCategory.LiquidBulk => "Tanque",
            _ => "Baú / Sider"
        };

        var refrigeration = product.TransportEnvironment switch
        {
            TransportEnvironment.Dry => "Nenhuma",
            TransportEnvironment.Chilled => "Resfriado",
            TransportEnvironment.Frozen => "Congelado",
            _ => "Nenhuma"
        };

        return new VehicleRequirement(
            BaseBodyworkType: bodywork,
            MinRefrigerationLevel: refrigeration,
            RequiresMopp: product.Dangerous,
            RequiresCargoFixing: product.Fragile);
    }
}
