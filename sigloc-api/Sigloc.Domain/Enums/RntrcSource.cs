namespace Sigloc.Domain.Enums;

/// <summary>
/// Whose RNTRC a vehicle runs under: the carrier's own (kept on the carrier profile) or a third
/// party's, typically an aggregated autonomous driver (TAC) or a cooperative.
/// </summary>
public enum RntrcSource
{
    Carrier = 1,
    ThirdParty = 2
}
