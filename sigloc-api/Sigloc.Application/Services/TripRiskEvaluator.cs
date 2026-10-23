using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>
/// Risk levels ordered NAO_MONITORADO &lt; NORMAL &lt; WARNING &lt; CRITIC. The stored snapshot risk only
/// reflects the ETA; the effective risk shown to users also includes open carrier occurrences.
/// </summary>
public static class TripRiskEvaluator
{
    public const string Unmonitored = "NAO_MONITORADO";
    public const string Normal = "NORMAL";
    public const string Warning = "WARNING";
    public const string Critical = "CRITIC";

    public static int Rank(string? risk) => risk switch
    {
        Critical => 3, Warning => 2, Normal => 1, _ => 0
    };

    public static string Max(string a, string b) => Rank(b) > Rank(a) ? b : a;

    public static string SeverityOf(TripOccurrenceType type) =>
        type is TripOccurrenceType.MechanicalFailure or TripOccurrenceType.SlaRisk ? Critical : Warning;

    public static string EtaRisk(DateTimeOffset? eta, DateTimeOffset? deadline) => eta > deadline ? Critical : Normal;

    public static string Effective(string? etaRisk, IEnumerable<TripOccurrence> occurrences) =>
        occurrences.Where(o => o.ResolvedAt == null).Select(o => o.Severity).Aggregate(etaRisk ?? Unmonitored, Max);
}
