namespace Sigloc.Domain.Entities;

public class TripTelemetry : BaseEntity
{
    public Guid TripId { get; set; }
    public string ObservationId { get; set; } = string.Empty;
    public long DeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset FixTime { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
}

public class TripMonitoringEvent : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid RefreshId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
