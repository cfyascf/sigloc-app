using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

/// <summary>Aggregated bid metrics for a single auction.</summary>
public sealed record AuctionBidMetrics(Guid AuctionId, decimal? BestBid, int TotalBids);

/// <summary>Best (lowest total) bid for an auction along with the owning carrier's name.</summary>
public sealed record BestBidWithCarrier(decimal TotalValue, string CarrierName);

/// <summary>
/// A single bid joined with the carrier and vehicle data needed to build the ranking
/// (Motor de Ranking) shown on the bid-analysis screen.
/// </summary>
public sealed record RankedBid(
    Bid Bid,
    Carrier Carrier,
    Vehicle Vehicle);

public interface IBidRepository
{
    /// <summary>Stages a new bid. Does not persist until the unit of work is saved.</summary>
    Task AddAsync(Bid bid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the best (minimum <c>TotalValue</c>) bid and the total bid count for each
    /// requested auction. Auctions with no bids are omitted; callers default them to
    /// (null, 0).
    /// </summary>
    Task<IReadOnlyList<AuctionBidMetrics>> GetMetricsForAuctionsAsync(
        IReadOnlyCollection<Guid> auctionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the best (minimum <c>TotalValue</c>) bid for the auction together with the
    /// name of the carrier that owns it, or null when the auction has no bids. Ties are
    /// broken by the earliest submission timestamp.
    /// </summary>
    Task<BestBidWithCarrier?> GetBestBidWithCarrierAsync(
        Guid auctionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the active bids of an auction joined with their carrier and vehicle,
    /// ordered by <c>TotalValue</c> ascending and, on ties, by the carrier's average
    /// rating descending (best-rated wins the higher rank). Withdrawn bids are excluded.
    /// </summary>
    Task<IReadOnlyList<RankedBid>> GetRankedBidsAsync(
        Guid auctionId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads all tracked bids of an auction so their status can be updated in a transaction.</summary>
    Task<IReadOnlyList<Bid>> GetTrackedByAuctionAsync(
        Guid auctionId,
        CancellationToken cancellationToken = default);
}
