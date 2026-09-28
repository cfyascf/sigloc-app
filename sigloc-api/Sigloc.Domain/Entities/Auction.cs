using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Commercial event (Leilão) that envelopes a consolidated route in a 1:1 relationship.
/// Carriers place bids against it until it expires.
/// </summary>
[Table("Auction")]
public class Auction : BaseEntity
{
    /// <summary>Consolidated route being auctioned (1:1).</summary>
    public Guid RouteId { get; set; }

    /// <summary>
    /// Optional custom label for the auction. When set it overrides the itinerary-derived
    /// name shown in the listing and detail; when null the name falls back to the route
    /// itinerary summary.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>Timestamp when the auction was opened.</summary>
    public DateTimeOffset OpenedAt { get; set; }

    /// <summary>Deadline captured from the "auction closing" input in the UI.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When true the winner is elected automatically at <see cref="ExpiresAt"/> by
    /// evaluating every bid together to secure the best possible price.
    /// </summary>
    public bool AutomaticAward { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Open;
}
