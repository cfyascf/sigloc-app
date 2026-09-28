using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Repositories;

/// <summary>
/// One auction paired with its consolidated route and the segments linked to that route.
/// Used to build both the listing itinerary and the detail travel plan.
/// </summary>
public sealed record AuctionWithRoute(
    Auction Auction,
    ConsolidatedRoute Route,
    IReadOnlyList<RouteSegment> Segments);

public interface IAuctionRepository
{
    /// <summary>Stages a new auction. Does not persist until the unit of work is saved.</summary>
    Task AddAsync(Auction auction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches auctions owned by the contractor, joined to their consolidated route and
    /// linked segments (with products). Results are ordered by <c>ExpiresAt</c> ascending
    /// so the auctions closest to expiring come first.
    /// </summary>
    Task<(IReadOnlyList<AuctionWithRoute> Items, int TotalItems)> SearchAsync(
        Guid contractorId,
        string? search,
        AuctionStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single auction (scoped to the contractor) with its consolidated route and
    /// linked segments (including products), or null when it does not exist or belongs to
    /// another contractor.
    /// </summary>
    Task<AuctionWithRoute?> GetDetailAsync(
        Guid id,
        Guid contractorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single tracked auction scoped to the contractor (via its consolidated
    /// route), or null when it does not exist or belongs to another contractor. The
    /// returned entity is change-tracked so mutations can be persisted.
    /// </summary>
    Task<Auction?> GetTrackedByIdAsync(
        Guid id,
        Guid contractorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single auction with its consolidated route and linked segments (including
    /// products) for the carrier bid workspace. Unlike <see cref="GetDetailAsync"/> this
    /// is NOT scoped to a contractor, since any carrier can analyse an open auction.
    /// Returns null when the auction does not exist.
    /// </summary>
    Task<AuctionWithRoute?> GetForCarrierAnalysisAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to a tracked auction.</summary>
    Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default);

    /// <summary>Deletes an auction. The consolidated route cascade is left untouched.</summary>
    Task DeleteAsync(Auction auction, CancellationToken cancellationToken = default);
}
