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
        [ProductCategory.LiquidBulk] = "GranelLiquido",
        [ProductCategory.PressurizedBulk] = "GranelPressurizado",
        [ProductCategory.Containerized] = "Conteinerizada",
        [ProductCategory.NeoBulk] = "Neogranel",
        [ProductCategory.LiveCargo] = "CargaViva"
    };

    private static readonly Dictionary<ProductCategory, string> CategoryToLabel = new()
    {
        [ProductCategory.General] = "Geral",
        [ProductCategory.SolidBulk] = "Granel Sólido",
        [ProductCategory.LiquidBulk] = "Granel Líquido",
        [ProductCategory.PressurizedBulk] = "Granel Pressurizado",
        [ProductCategory.Containerized] = "Conteinerizada",
        [ProductCategory.NeoBulk] = "Neogranel",
        [ProductCategory.LiveCargo] = "Carga Viva"
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

    /// <summary>User-facing name, e.g. "Granel Sólido".</summary>
    public static string ToLabel(this ProductCategory value) => CategoryToLabel[value];

    /// <summary>"'Geral', 'GranelSolido' ou 'CargaViva'": every accepted wire value, for error messages.</summary>
    public static string CategoryWireValues() => JoinQuoted(CategoryToWire.Values);

    public static string ToWire(this TransportEnvironment value) => EnvironmentToWire[value];

    public static string ToWire(this PackagingType value) => PackagingToWire[value];

    public static bool TryParseCategory(string? value, out ProductCategory result)
        => TryParseWire(CategoryToWire, value, out result);

    public static bool TryParseEnvironment(string? value, out TransportEnvironment result)
        => TryParseWire(EnvironmentToWire, value, out result);

    public static bool TryParsePackaging(string? value, out PackagingType result)
        => TryParseWire(PackagingToWire, value, out result);

    private static string JoinQuoted(IEnumerable<string> values)
    {
        var quoted = values.Select(v => $"'{v}'").ToList();
        return quoted.Count == 1 ? quoted[0] : $"{string.Join(", ", quoted[..^1])} ou {quoted[^1]}";
    }

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
