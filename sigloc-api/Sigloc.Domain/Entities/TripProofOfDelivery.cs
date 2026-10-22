namespace Sigloc.Domain.Entities;

/// <summary>
/// Proof of delivery (canhoto) attached when a carrier confirms a delivery stop. The file itself lives
/// in file storage under <see cref="StorageKey"/>; only metadata is kept here.
/// </summary>
public class TripProofOfDelivery : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid TripStopId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    /// <summary>Sanitized display name; never used to build storage paths.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Content type detected from the file signature, not the client header.</summary>
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
