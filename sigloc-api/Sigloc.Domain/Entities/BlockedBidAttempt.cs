using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Audit entry logged whenever the anti-overbooking engine blocks a bid/allocation
/// attempt (by SLA, volume or weight). The executive dashboard counts the current
/// month's entries per contractor to surface the <c>BlockedOverbookings</c> KPI.
/// </summary>
[Table("BlockedBidAttempt")]
public class BlockedBidAttempt : BaseEntity
{
    /// <summary>Owning contractor (Contratante) whose auction the attempt targeted.</summary>
    public Guid ContractorId { get; set; }

    /// <summary>Auction (Leilão) the blocked attempt was placed against.</summary>
    public Guid AuctionId { get; set; }

    /// <summary>Carrier (Transportadora) that attempted the bid, when known.</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Why the attempt was blocked (SLA, volume or weight).</summary>
    public BlockedReason Reason { get; set; }

    /// <summary>When the attempt was made and blocked.</summary>
    public DateTimeOffset AttemptedAt { get; set; }
}
