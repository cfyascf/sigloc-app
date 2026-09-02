namespace Sigloc.Domain.Enums;

/// <summary>
/// Maps between the API wire values (defined by the product specification) and the
/// internal English enums. The wire values are kept exactly as specified so the
/// JSON contract is preserved while the domain model uses English identifiers.
/// </summary>
public static class ProductEnumMappings
{
    private static readonly Dictionary<ProductCategory, string> CategoryToWire = new()
    {
        [ProductCategory.General] = "Geral",
        [ProductCategory.SolidBulk] = "GranelSolido",
        [ProductCategory.LiquidBulk] = "GranelLiquido"
    };

    private static readonly Dictionary<TransportEnvironment, string> EnvironmentToWire = new()
    {
        [TransportEnvironment.Dry] = "Seco",
        [TransportEnvironment.Chilled] = "Resfriado",
        [TransportEnvironment.Frozen] = "Congelado"
    };

    private static readonly Dictionary<PackagingType, string> PackagingToWire = new()
    {
        [PackagingType.Palletized] = "Paletizado",
        [PackagingType.MasterCartons] = "Caixas Master",
        [PackagingType.BagsSacks] = "Sacaria/Bag"
    };

    public static string ToWire(this ProductCategory value) => CategoryToWire[value];

    public static string ToWire(this TransportEnvironment value) => EnvironmentToWire[value];

    public static string ToWire(this PackagingType value) => PackagingToWire[value];

    public static bool TryParseCategory(string? value, out ProductCategory result)
        => TryParseWire(CategoryToWire, value, out result);

    public static bool TryParseEnvironment(string? value, out TransportEnvironment result)
        => TryParseWire(EnvironmentToWire, value, out result);

    public static bool TryParsePackaging(string? value, out PackagingType result)
        => TryParseWire(PackagingToWire, value, out result);

    private static bool TryParseWire<TEnum>(Dictionary<TEnum, string> map, string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            foreach (var pair in map)
            {
                if (string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = pair.Key;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}
