namespace Sigloc.Domain.Enums;

/// <summary>
/// Lifecycle state of an auction (Leilão) that envelopes a consolidated route.
/// </summary>
public enum AuctionStatus
{
    Open,
    Closed,

    /// <summary>
    /// The auction was deleted without choosing a winner. It is kept for history as
    /// "Excluído"; the linked route and segments remain consumed.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The freight offer was undone. It is kept for history as "Desfeito" and its route
    /// segments are released back to <see cref="SegmentStatus.Available"/> for reuse.
    /// </summary>
    Undone
}
