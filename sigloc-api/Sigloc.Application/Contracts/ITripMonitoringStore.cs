using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
namespace Sigloc.Application.Contracts;

public record TripState(Trip Trip, ConsolidatedRoute Route, Vehicle Vehicle, Carrier Carrier,
    List<RouteSegment> Segments, TripMonitoring? Monitoring, List<TripMonitoringEvent> Events)
{
    public TripMonitoring? Snapshot { get; set; } = Monitoring;
    public List<TripTelemetry> NewTelemetry { get; } = new();
}

public interface ITripMonitoringStore
{
    Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct);
    Task<TripState?> ReadAsync(Guid contractorId, Guid tripId, CancellationToken ct);
    Task<TripDetailDto> RefreshAsync(Guid contractorId, Guid tripId,
        Func<TripState, CancellationToken, Task<TripDetailDto>> refresh, CancellationToken ct);
}

public sealed class TripMonitoringOptions
{
    public int MaxGpsAgeMinutes { get; set; } = 15;
    public double GeofenceRadiusMeters { get; set; } = 200;
    public int DockBufferMinutes { get; set; } = 30;
}
