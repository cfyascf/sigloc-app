using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IAuctionService
{
    Task<AuctionResponseDto> CreateAsync(CreateAuctionDto dto, CancellationToken cancellationToken = default);
    Task<AuctionResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuctionResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AuctionResponseDto> UpdateAsync(Guid id, UpdateAuctionDto dto, CancellationToken cancellationToken = default);
    Task<AuctionResponseDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}