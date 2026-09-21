using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IRouteSegmentService
{
    Task<RouteSegmentResponseDto> CreateAsync(Guid contractorId, RouteSegmentRequestDto dto, CancellationToken cancellationToken = default);
    Task<RouteSegmentResponseDto> GetByIdAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);
    Task<PagedRouteSegmentsDto> SearchAsync(Guid contractorId, RouteSegmentQueryDto query, CancellationToken cancellationToken = default);
    Task<RouteSegmentResponseDto> UpdateAsync(Guid contractorId, Guid id, RouteSegmentRequestDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default);
}
