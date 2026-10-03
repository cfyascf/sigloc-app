using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Volatile monitoring snapshot (MONITORAMENTO) for an in-progress trip (Viagem). Holds
/// the last progress, the last ETA calculated by the routing engine (OpenRouteService)
/// and the last ping timestamp. Consumed by the executive dashboard SLA countdown; the
/// ETA is refreshed lazily/on-demand and cached here (1:1 with the trip).
/// </summary>
[Table("TripMonitoring")]
public class TripMonitoring : BaseEntity
{
    /// <summary>Trip (Viagem) being monitored (1:1).</summary>
    public Guid TripId { get; set; }

    /// <summary>Last known route progress as a percentage (0-100).</summary>
    public double LastProgressPercentage { get; set; }

    /// <summary>Last ETA calculated by the routing engine for the current leg.</summary>
    public DateTimeOffset? LastCalculatedEta { get; set; }

    /// <summary>Successful calculation time, separate from the GPS fix time.</summary>
    public DateTimeOffset? LastSuccessfulCalculationAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? TraveledDistanceKm { get; set; }
    public double? RemainingDistanceKm { get; set; }
    public Guid? NextStopId { get; set; }
    public DateTimeOffset? NextStopDeadline { get; set; }
    public string? Risk { get; set; }
    public string? LastObservationId { get; set; }
    public long? LastDeviceId { get; set; }

    /// <summary>Timestamp of the last location ping received for the trip.</summary>
    public DateTimeOffset LastPingAt { get; set; }
}
