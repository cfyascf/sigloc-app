namespace Sigloc.Domain.Entities;

/// <summary>
/// Handling class of a load. Dangerous, fragile and common goods are never carried together:
/// a segment holds a single class and a consolidated route only joins segments of the same class.
/// </summary>
public enum CargoClass
{
    Comum,
    Fragil,
    Perigosa,
}

public static class CargoClassification
{
    /// <summary>A product that is both dangerous and fragile is handled as dangerous.</summary>
    public static CargoClass Of(Product product) =>
        product.Dangerous ? CargoClass.Perigosa : product.Fragile ? CargoClass.Fragil : CargoClass.Comum;

    /// <summary>Distinct classes present in the products, most restrictive first.</summary>
    public static IReadOnlyList<CargoClass> ClassesOf(IEnumerable<Product> products) =>
        products.Select(Of).Distinct().OrderByDescending(c => c).ToList();

    /// <summary>Wire value: COMUM, FRAGIL, PERIGOSA, or MISTA for a load that mixes classes (older data).</summary>
    public static string WireValue(IEnumerable<Product> products)
    {
        var classes = ClassesOf(products);
        return classes.Count switch
        {
            0 => "COMUM",
            1 => classes[0].ToString().ToUpperInvariant(),
            _ => "MISTA",
        };
    }

    public static string Label(CargoClass cargoClass) => cargoClass switch
    {
        CargoClass.Perigosa => "carga perigosa",
        CargoClass.Fragil => "carga frágil",
        _ => "carga comum",
    };

    /// <summary>"carga perigosa e carga comum"</summary>
    public static string Describe(IReadOnlyList<CargoClass> classes) =>
        string.Join(" e ", classes.Select(Label));
}
