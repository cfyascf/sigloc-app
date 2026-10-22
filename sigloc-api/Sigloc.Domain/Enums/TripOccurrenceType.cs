namespace Sigloc.Domain.Enums;

/// <summary>Operational exception a driver reports while a trip is in progress.</summary>
public enum TripOccurrenceType
{
    HeavyTraffic,
    MissingDocumentation,
    MechanicalFailure,
    SlaRisk
}
