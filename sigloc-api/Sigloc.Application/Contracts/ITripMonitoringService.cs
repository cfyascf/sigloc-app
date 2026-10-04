using Sigloc.Application.DTOs;
namespace Sigloc.Application.Contracts;
public interface ITripMonitoringService
{
    Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct = default);
    Task<TripDetailDto> GetDetailAsync(Guid contractorId, Guid tripId, bool refresh = false, CancellationToken ct = default);
}
