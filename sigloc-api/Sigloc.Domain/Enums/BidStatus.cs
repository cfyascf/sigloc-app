namespace Sigloc.Domain.Enums;

/// <summary>
/// Dynamic state of a bid (Lance) placed by a carrier against an auction.
/// </summary>
public enum BidStatus
{
    /// <summary>Bid submitted and awaiting evaluation.</summary>
    Pending,

    /// <summary>Currently holding the best (lowest total) position.</summary>
    Winning,

    /// <summary>Outbid by another carrier while the auction is still open.</summary>
    Losing,

    /// <summary>Elected as the winner when the auction closed.</summary>
    Winner,

    /// <summary>Withdrawn by the carrier before the auction closed.</summary>
    Withdrawn
}
