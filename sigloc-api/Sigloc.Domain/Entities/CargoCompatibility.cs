using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// The single rule table matching cargo to vehicles: which body types carry each product category,
/// which refrigeration each body type can have, and what fragile cargo demands. Product validation,
/// vehicle validation and the bid compatibility check all read from here.
/// </summary>
public static class CargoCompatibility
{
    private static readonly IReadOnlyDictionary<ProductCategory, VehicleBodyType[]> BodiesByCategory =
        new Dictionary<ProductCategory, VehicleBodyType[]>
        {
            [ProductCategory.General] =
            [
                VehicleBodyType.Bau, VehicleBodyType.Frigorifico, VehicleBodyType.Sider,
                VehicleBodyType.GradeBaixa, VehicleBodyType.Graneleiro, VehicleBodyType.Prancha,
            ],
            [ProductCategory.SolidBulk] = [VehicleBodyType.Graneleiro, VehicleBodyType.Cacamba],
            [ProductCategory.LiquidBulk] = [VehicleBodyType.Tanque],
            [ProductCategory.PressurizedBulk] = [VehicleBodyType.Silo],
            [ProductCategory.Containerized] = [VehicleBodyType.PortaConteiner],
            [ProductCategory.NeoBulk] = [VehicleBodyType.GradeBaixa, VehicleBodyType.Prancha, VehicleBodyType.Cegonheiro],
            [ProductCategory.LiveCargo] = [VehicleBodyType.Gaiola],
        };

    // Refrigerated box for chilled or frozen loads; insulated tanks for chilled liquids (milk); reefer
    // containers for both. Every other body runs at ambient temperature.
    private static readonly IReadOnlyDictionary<VehicleBodyType, RefrigerationLevel[]> RefrigerationByBody =
        new Dictionary<VehicleBodyType, RefrigerationLevel[]>
        {
            [VehicleBodyType.Frigorifico] = [RefrigerationLevel.Resfriado, RefrigerationLevel.Congelado],
            [VehicleBodyType.Tanque] = [RefrigerationLevel.Nenhuma, RefrigerationLevel.Resfriado],
            [VehicleBodyType.PortaConteiner] = [RefrigerationLevel.Nenhuma, RefrigerationLevel.Resfriado, RefrigerationLevel.Congelado],
        };

    private static readonly RefrigerationLevel[] AmbientOnly = [RefrigerationLevel.Nenhuma];

    /// <summary>Open bodies leave the load exposed, so they can't take fragile cargo.</summary>
    private static readonly HashSet<VehicleBodyType> OpenBodies =
    [
        VehicleBodyType.GradeBaixa, VehicleBodyType.Graneleiro, VehicleBodyType.Cacamba,
        VehicleBodyType.Prancha, VehicleBodyType.Cegonheiro,
    ];

    /// <summary>
    /// Open platforms carrying unit loads must tie them down (CONTRAN Res. 945/2022), so cargo
    /// securing is mandatory on them.
    /// </summary>
    private static readonly HashSet<VehicleBodyType> SecuringRequiredBodies =
        [VehicleBodyType.GradeBaixa, VehicleBodyType.Prancha, VehicleBodyType.Cegonheiro];

    /// <summary>Fragile bulk or fragile live cargo makes no sense: only unit loads can be fragile.</summary>
    private static readonly HashSet<ProductCategory> FragileCategories =
        [ProductCategory.General, ProductCategory.Containerized];

    /// <summary>
    /// Bulk loads fill a single tank, hold or silo, and live animals share one cage: different
    /// products would contaminate or harm each other, and the vehicle can't take another segment's
    /// load. A segment of these categories carries one product and is offered on its own.
    /// </summary>
    private static readonly HashSet<ProductCategory> SingleProductCategories =
    [
        ProductCategory.SolidBulk, ProductCategory.LiquidBulk, ProductCategory.PressurizedBulk, ProductCategory.LiveCargo,
    ];

    /// <summary>Bulk cargo, carried in a tank, silo or open hold. Dangerous bulk needs CIV/CIPP (ANTT Res. 5.998/2022).</summary>
    public static bool IsBulk(ProductCategory category) =>
        category is ProductCategory.SolidBulk or ProductCategory.LiquidBulk or ProductCategory.PressurizedBulk;

    /// <summary>True when a segment of this category holds one product and can't join other segments in a route.</summary>
    public static bool IsSingleProduct(ProductCategory category) => SingleProductCategories.Contains(category);

    /// <summary>Body types that carry the category, before refrigeration and fragility are considered.</summary>
    public static IReadOnlyList<VehicleBodyType> BodiesFor(ProductCategory category) => BodiesByCategory[category];

    /// <summary>
    /// Body types that can carry a load of this category at this environment, excluding open bodies
    /// when the load is fragile.
    /// </summary>
    public static IReadOnlyList<VehicleBodyType> AcceptedBodies(ProductCategory category, TransportEnvironment environment, bool fragile)
    {
        var required = RequiredRefrigeration(environment);
        return BodiesByCategory[category]
            .Where(body => MaxRefrigeration(body) >= required)
            .Where(body => !fragile || !OpenBodies.Contains(body))
            .ToList();
    }

    /// <summary>Refrigeration levels a vehicle with this body may declare.</summary>
    public static IReadOnlyList<RefrigerationLevel> RefrigerationLevelsFor(VehicleBodyType bodyType) =>
        RefrigerationByBody.GetValueOrDefault(bodyType, AmbientOnly);

    /// <summary>
    /// Environments a product of this category can be shipped in: as cold as the coldest body
    /// that carries the category.
    /// </summary>
    public static IReadOnlyList<TransportEnvironment> EnvironmentsFor(ProductCategory category)
    {
        var coldest = BodiesByCategory[category].Max(MaxRefrigeration);
        return Enum.GetValues<TransportEnvironment>().Where(e => RequiredRefrigeration(e) <= coldest).ToList();
    }

    public static bool AllowsFragile(ProductCategory category) => FragileCategories.Contains(category);

    /// <summary>Live animals are never dangerous goods.</summary>
    public static bool AllowsDangerous(ProductCategory category) => category != ProductCategory.LiveCargo;

    public static bool RequiresCargoSecuring(VehicleBodyType bodyType) => SecuringRequiredBodies.Contains(bodyType);

    /// <summary>
    /// Refrigeration the vehicle can actually provide: the declared level capped by what its body
    /// supports. Vehicles saved before the levels were tied to the body (e.g. a Sider marked
    /// "Congelado") are treated as the body allows.
    /// </summary>
    public static RefrigerationLevel EffectiveRefrigeration(Vehicle vehicle)
    {
        var max = MaxRefrigeration(vehicle.BodyType);
        return vehicle.RefrigerationLevel > max ? max : vehicle.RefrigerationLevel;
    }

    public static RefrigerationLevel RequiredRefrigeration(TransportEnvironment environment) => environment switch
    {
        TransportEnvironment.Frozen => RefrigerationLevel.Congelado,
        TransportEnvironment.Chilled => RefrigerationLevel.Resfriado,
        _ => RefrigerationLevel.Nenhuma,
    };

    /// <summary>"Baú, Sider ou Grade Baixa"; "Nenhuma carroceria compatível" when the list is empty.</summary>
    public static string Describe(IReadOnlyList<VehicleBodyType> bodies)
    {
        var labels = bodies.Select(b => b.ToLabel()).ToList();
        return labels.Count switch
        {
            0 => "Nenhuma carroceria compatível",
            1 => labels[0],
            _ => $"{string.Join(", ", labels[..^1])} ou {labels[^1]}",
        };
    }

    private static RefrigerationLevel MaxRefrigeration(VehicleBodyType bodyType) => RefrigerationLevelsFor(bodyType).Max();
}
