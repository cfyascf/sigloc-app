using System.ComponentModel;
using System.Reflection;

namespace Sigloc.Domain.Enums;

public static class VehicleEnumMappings
{
    /// <summary>User-facing name of a body type, from its [Description] attribute (e.g. "Baú Refrigerado").</summary>
    public static string ToLabel(this VehicleBodyType bodyType) => Describe(bodyType);

    /// <summary>"Cavalo + carreta"</summary>
    public static string ToLabel(this VehicleComposition composition) => Describe(composition);

    /// <summary>"TAC (Transportador Autônomo)"</summary>
    public static string ToLabel(this RntrcCategory category) => Describe(category);

    private static string Describe<TEnum>(TEnum value) where TEnum : struct, Enum =>
        typeof(TEnum).GetField(value.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description
        ?? value.ToString();
}
