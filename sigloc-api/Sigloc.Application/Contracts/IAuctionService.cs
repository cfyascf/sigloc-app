using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IAuctionService
{
    /// <summary>
    /// Creates the consolidated route snapshot, links the segments and opens the auction
    /// inside a single atomic transaction, then notifies partner carriers.
    /// </summary>
    Task<CreateAuctionResponseDto> CreateAsync(Guid contractorId, CreateAuctionRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the contractor's auctions with the consolidated itinerary, aggregated bid
    /// metrics and the on-demand risk indicator, ordered by expiry (soonest first).
    /// </summary>
    Task<PagedAuctionsDto> SearchAsync(Guid contractorId, AuctionQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the deep route detail for a single auction: financial scenario, best bid
    /// with the winning carrier, the Milking Run travel plan and the linked segments.
    /// </summary>
    Task<AuctionDetailDto> GetDetailAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the auction's editable metadata (custom name and/or bid deadline) for the
    /// owning contractor. Only the provided fields are changed.
    /// </summary>
    Task<UpdateAuctionResponseDto> UpdateAsync(Guid contractorId, Guid id, UpdateAuctionRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes (soft inactivates) an open auction for the owning contractor without
    /// selecting a winner: the auction is kept with status <c>Cancelled</c> so it remains
    /// visible in the history as "Excluído". Its route and segments remain consumed.
    /// </summary>
    Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes an open freight offer for the owning contractor without selecting a winner:
    /// the auction is kept with status <c>Undone</c> so it remains visible in the history as
    /// "Desfeito", and its consolidated route and segments are released back to
    /// <c>Available</c> for reuse.
    /// </summary>
    Task UndoAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ranked list of active bids for the auction (menor valor primeiro; empate
    /// desempatado pela nota média), with the real-time savings calculated against the
    /// consolidated ceiling and the carrier reliability data.
    /// </summary>
    Task<BidRankingDto> GetBidRankingAsync(Guid contractorId, Guid auctionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Awards the auction to the chosen bid inside a single atomic transaction: marks the
    /// winner and the losers, closes the auction, moves the route to awaiting pickup and
    /// creates the trip (Viagem) that starts the physical operation.
    /// </summary>
    Task<AwardAuctionResponseDto> AwardAsync(Guid contractorId, Guid auctionId, AwardAuctionRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the carrier bid workspace (Workspace de Lance): the deep route data, the
    /// competition snapshot, the cargo-inherited physical/compliance requirements and the
    /// carrier's full available fleet.
    /// </summary>
    Task<CarrierBidAnalysisDto> GetCarrierAnalysisAsync(Guid carrierId, Guid auctionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a carrier bid after running the Constraint Engine (Hard Block): financial,
    /// weight, volume, equipment/compliance and anti-overbooking travas. Rejected attempts
    /// throw a <see cref="Sigloc.Application.Exceptions.BidRejectedException"/> and are logged
    /// for audit; a valid bid is persisted and returned (201).
    /// </summary>
    Task<PlaceBidResponseDto> PlaceBidAsync(Guid carrierId, Guid auctionId, PlaceBidRequestDto dto, CancellationToken cancellationToken = default);
}
