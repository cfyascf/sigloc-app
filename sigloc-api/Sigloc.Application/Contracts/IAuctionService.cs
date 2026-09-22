using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IAuctionService
{
    /// <summary>
    /// Creates the consolidated route snapshot, links the segments and opens the auction
    /// inside a single atomic transaction, then notifies partner carriers.
    /// </summary>
    Task<CreateAuctionResponseDto> CreateAsync(Guid contractorId, CreateAuctionRequestDto dto, CancellationToken cancellationToken = default);
}
