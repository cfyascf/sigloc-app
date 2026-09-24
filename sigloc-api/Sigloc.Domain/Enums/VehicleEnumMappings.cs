namespace Sigloc.Domain.Enums;

/// <summary>
/// Maps between the API wire values and the internal vehicle enums. The wire values are
/// kept stable so the JSON contract is preserved while the domain model uses English
/// identifiers, mirroring <see cref="ProductEnumMappings"/> and <see cref="SegmentEnumMappings"/>.
/// </summary>
public static class VehicleEnumMappings
{
    private static readonly Dictionary<VehicleBodyType, string> BodyTypeToWire = new()
    {
        [VehicleBodyType.Bau] = "Bau",
        [VehicleBodyType.Sider] = "Sider",
        [VehicleBodyType.GradeBaixa] = "GradeBaixa",
        [VehicleBodyType.Frigorifico] = "Frigorifico",
        [VehicleBodyType.Cacamba] = "Cacamba"
    };

    private static readonly Dictionary<RefrigerationLevel, string> RefrigerationToWire = new()
    {
        [RefrigerationLevel.Nenhuma] = "Nenhuma",
        [RefrigerationLevel.Resfriado] = "Resfriado",
        [RefrigerationLevel.Congelado] = "Congelado"
    };

    private static readonly Dictionary<OperationalStatus, string> StatusToWire = new()
    {
        [OperationalStatus.LIVRE] = "Livre",
        [OperationalStatus.EM_TRANSITO] = "EmTransito",
        [OperationalStatus.MANUTENCAO] = "Manutencao"
    };

    public static string ToWire(this VehicleBodyType value) => BodyTypeToWire[value];

    public static string ToWire(this RefrigerationLevel value) => RefrigerationToWire[value];

    public static string ToWire(this OperationalStatus value) => StatusToWire[value];

    public static bool TryParseBodyType(string? value, out VehicleBodyType result)
        => TryParseWire(BodyTypeToWire, value, out result);

    public static bool TryParseRefrigeration(string? value, out RefrigerationLevel result)
        => TryParseWire(RefrigerationToWire, value, out result);

    public static bool TryParseStatus(string? value, out OperationalStatus result)
        => TryParseWire(StatusToWire, value, out result);

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
