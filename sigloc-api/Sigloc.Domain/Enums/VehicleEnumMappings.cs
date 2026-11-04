using System.ComponentModel;
using System.Reflection;

namespace Sigloc.Domain.Enums;

public static class VehicleEnumMappings
{
    /// <summary>User-facing name of a body type, from its [Description] attribute (e.g. "Baú Refrigerado").</summary>
    public static string ToLabel(this VehicleBodyType bodyType) =>
        typeof(VehicleBodyType).GetField(bodyType.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description
        ?? bodyType.ToString();
}
