using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IConsolidatedRouteService
{
    Task<ConsolidatedRouteResponseDto> CreateAsync(CreateConsolidatedRouteDto dto, CancellationToken cancellationToken = default);
    Task<ConsolidatedRouteResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConsolidatedRouteResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ConsolidatedRouteResponseDto> UpdateAsync(Guid id, UpdateConsolidatedRouteDto dto, CancellationToken cancellationToken = default);
    Task<ConsolidatedRouteResponseDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}