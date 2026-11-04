namespace Sigloc.Domain.Enums;

/// <summary>
/// Cargo type, following the ANTT cargo types (Res. 5.867/2020) plus live cargo. Refrigerated and
/// dangerous cargo are not categories here: they are the product's transport environment and
/// dangerous flag. Persisted by name, so new values only need to be appended.
/// </summary>
public enum ProductCategory
{
    General,
    SolidBulk,
    LiquidBulk,
    PressurizedBulk,
    Containerized,
    NeoBulk,
    LiveCargo
}
