namespace Sigloc.Domain.Enums;

/// <summary>Maps occurrence types to the pt-BR API wire values and display labels.</summary>
public static class TripOccurrenceMappings
{
    private static readonly Dictionary<TripOccurrenceType, string> TypeToWire = new()
    {
        [TripOccurrenceType.HeavyTraffic] = "TRANSITO_INTENSO",
        [TripOccurrenceType.MissingDocumentation] = "FALTA_DOCUMENTACAO",
        [TripOccurrenceType.MechanicalFailure] = "AVARIA_MECANICA",
        [TripOccurrenceType.SlaRisk] = "RISCO_SLA"
    };

    private static readonly Dictionary<TripOccurrenceType, string> TypeToLabel = new()
    {
        [TripOccurrenceType.HeavyTraffic] = "Trânsito Intenso",
        [TripOccurrenceType.MissingDocumentation] = "Falta de Documentação",
        [TripOccurrenceType.MechanicalFailure] = "Avaria Mecânica",
        [TripOccurrenceType.SlaRisk] = "Risco de Extrapolar SLA"
    };

    public static string ToWire(this TripOccurrenceType value) => TypeToWire[value];

    public static string ToLabel(this TripOccurrenceType value) => TypeToLabel[value];

    public static bool TryParseOccurrenceType(string? value, out TripOccurrenceType result)
    {
        foreach (var (type, wire) in TypeToWire)
        {
            if (string.Equals(wire, value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                result = type;
                return true;
            }
        }

        result = default;
        return false;
    }
}
