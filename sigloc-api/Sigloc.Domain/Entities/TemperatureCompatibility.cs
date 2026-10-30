using System.Globalization;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>Temperature range (°C) a load tolerates during transport.</summary>
public readonly record struct TemperatureRange(double Min, double Max);

/// <summary>
/// A compartment runs at a single temperature, so a segment or a consolidated route only joins
/// products whose temperature ranges overlap. Dry products have no range of their own and travel
/// at ambient temperature: they can go with chilled loads but not with frozen ones.
/// </summary>
public static class TemperatureCompatibility
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Range assumed for dry products.</summary>
    public static readonly TemperatureRange Ambient = new(0, 35);

    /// <summary>
    /// Range the product tolerates. A chilled/frozen product missing a bound (older data) is left
    /// open on that side.
    /// </summary>
    public static TemperatureRange RangeOf(Product product) =>
        product.TransportEnvironment == TransportEnvironment.Dry
            ? Ambient
            : new(product.TempMin ?? double.NegativeInfinity, product.TempMax ?? double.PositiveInfinity);

    /// <summary>
    /// Null when one temperature fits every product. Otherwise the pair that can't share it: the
    /// product with the warmest minimum and the one with the coldest maximum.
    /// </summary>
    public static (Product Warmest, Product Coldest)? FindConflict(IEnumerable<Product> products)
    {
        var list = products.ToList();
        if (list.Count < 2)
        {
            return null;
        }

        var warmest = list.MaxBy(p => RangeOf(p).Min)!;
        var coldest = list.MinBy(p => RangeOf(p).Max)!;
        return RangeOf(warmest).Min > RangeOf(coldest).Max ? (warmest, coldest) : null;
    }

    /// <summary>"Sorvete (-25 °C a -18 °C)", "Arroz (ambiente, 0 °C a 35 °C)"</summary>
    public static string Describe(Product product)
    {
        var range = RangeOf(product);
        var label = (double.IsFinite(range.Min), double.IsFinite(range.Max)) switch
        {
            (true, true) => $"{Degrees(range.Min)} a {Degrees(range.Max)}",
            (true, false) => $"a partir de {Degrees(range.Min)}",
            (false, true) => $"até {Degrees(range.Max)}",
            _ => "qualquer temperatura",
        };
        return product.TransportEnvironment == TransportEnvironment.Dry
            ? $"{product.Name} (ambiente, {label})"
            : $"{product.Name} ({label})";
    }

    private static string Degrees(double value) => $"{value.ToString("0.#", PtBr)} °C";
}
