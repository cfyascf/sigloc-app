namespace Sigloc.Domain.Enums;

/// <summary>
/// Lifecycle view applied to the carrier-facing opportunity board (Mural de Fretes).
/// </summary>
public enum FreightOfferBoardFilter
{
    /// <summary>Open, non-expired auctions still accepting bids (default board).</summary>
    Open,

    /// <summary>Closed, cancelled or expired auctions kept for history.</summary>
    Closed
}
