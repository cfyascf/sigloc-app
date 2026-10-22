using Sigloc.Application.DTOs;
namespace Sigloc.Application.Contracts;
public interface ITripMonitoringService
{
    Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct = default);
    Task<TripDetailDto> GetDetailAsync(Guid contractorId, Guid tripId, bool refresh = false, CancellationToken ct = default);

    Task<PagedTripsDto> SearchAsync(TripScope scope, TripQueryDto query, CancellationToken ct = default);

    /// <summary>
    /// Same read/refresh pipeline as the shipper detail, scoped to any party. <paramref name="project"/>
    /// receives the loaded graph and its shared detail DTO so callers can enrich the response.
    /// </summary>
    Task<T> GetDetailAsync<T>(TripScope scope, Guid tripId, bool refresh,
        Func<TripState, TripDetailDto, T> project, CancellationToken ct = default);
}
