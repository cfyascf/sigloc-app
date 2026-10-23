using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Operational exception reported by the carrier. While open it raises the trip's effective risk to
/// <see cref="Severity"/>; it closes automatically when <see cref="BlockingStopId"/> is completed.
/// </summary>
public class TripOccurrence : BaseEntity
{
    public Guid TripId { get; set; }
    public TripOccurrenceType Type { get; set; }
    /// <summary>Risk level the occurrence imposes while open: WARNING or CRITIC.</summary>
    public string Severity { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid ReportedByUserId { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
    /// <summary>First pending stop when reported; null when nothing was pending.</summary>
    public Guid? BlockingStopId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionSource { get; set; }
}
