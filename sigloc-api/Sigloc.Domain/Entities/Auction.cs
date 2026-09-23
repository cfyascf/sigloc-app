using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Auction (Leilão): the commercial event that wraps a consolidated route and is
/// offered to the contractor's carrier network. 1:1 with <see cref="ConsolidatedRoute"/>.
/// </summary>
[Table("Auction")]
public class Auction : BaseEntity
{
    /// <summary>Consolidated route this auction offers. 1:1.</summary>
    public Guid RouteId { get; set; }

    public DateTimeOffset OpenedAt { get; set; }

    /// <summary>Captured from the "Encerramento do Leilão" input on the UI.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>"Seleção automática do vencedor" toggle from the UI.</summary>
    public bool AutomaticAward { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Open;
}
