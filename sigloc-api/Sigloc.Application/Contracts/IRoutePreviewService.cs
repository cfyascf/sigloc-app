using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IRoutePreviewService
{
    /// <summary>
    /// Simulates consolidating the given route segments into a route. Read-only: no
    /// data is written and no segment status is checked.
    /// </summary>
    Task<RoutePreviewResponseDto> PreviewAsync(Guid contractorId, RoutePreviewRequestDto dto, CancellationToken cancellationToken = default);
}
