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

    /// <summary>Deletes the auction for the owning contractor.</summary>
    Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);
}
