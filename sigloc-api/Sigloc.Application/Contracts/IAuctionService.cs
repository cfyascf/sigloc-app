using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IAuctionService
{
    /// <summary>
    /// Atomically consolidates the given route segments into a new route, links them,
    /// and opens an auction for it. Throws <see cref="Sigloc.Application.Exceptions.RouteSegmentUnavailableException"/>
    /// if any segment is not Available.
    /// </summary>
    Task<CreateAuctionResponseDto> CreateAsync(Guid contractorId, CreateAuctionRequestDto dto, CancellationToken cancellationToken = default);
}
