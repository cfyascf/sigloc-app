using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
namespace Sigloc.Application.Contracts;

public record TripState(Trip Trip, ConsolidatedRoute Route, Vehicle Vehicle, Carrier Carrier,
    List<RouteSegment> Segments, TripMonitoring? Monitoring, List<TripMonitoringEvent> Events)
{
    public TripMonitoring? Snapshot { get; set; } = Monitoring;
    public List<TripTelemetry> NewTelemetry { get; } = new();
    /// <summary>Every occurrence of the trip, newest first; new ones are persisted on update.</summary>
    public List<TripOccurrence> Occurrences { get; init; } = new();
    /// <summary>Proofs of delivery of the trip, newest first; new ones are persisted on update.</summary>
    public List<TripProofOfDelivery> Proofs { get; init; } = new();
    /// <summary>Shipper that owns the route (contact shown to the carrier).</summary>
    public Contractor? Shipper { get; init; }
}

public interface ITripMonitoringStore
{
    Task<PagedTripsDto> SearchAsync(TripScope scope, TripQueryDto query, CancellationToken ct);
    Task<TripState?> ReadAsync(TripScope scope, Guid tripId, CancellationToken ct);

    /// <summary>
    /// Runs <paramref name="mutate"/> against a freshly loaded graph while holding the trip row lock and
    /// commits every change it made atomically. The callback may be invoked again on a transient retry.
    /// </summary>
    Task<T> UpdateAsync<T>(TripScope scope, Guid tripId,
        Func<TripState, CancellationToken, Task<T>> mutate, CancellationToken ct);
}

public sealed class TripMonitoringOptions
{
    public int MaxGpsAgeMinutes { get; set; } = 15;
    public double GeofenceRadiusMeters { get; set; } = 200;
    public int DockBufferMinutes { get; set; } = 30;
}
