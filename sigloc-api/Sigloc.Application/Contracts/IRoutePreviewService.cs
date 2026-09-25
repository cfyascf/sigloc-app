using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IRoutePreviewService
{
    /// <summary>
    /// Simulates a consolidated route from the given segment ids without writing to the
    /// database. Aggregates weight, volume, financials and the most restrictive vehicle
    /// requirement, and sums the stored segment distances with the inter-segment legs.
    /// </summary>
    Task<RoutePreviewResponseDto> PreviewAsync(Guid contractorId, RoutePreviewRequestDto dto, CancellationToken cancellationToken = default);
}
