namespace Sigloc.Domain.Entities;

public class TripStop : BaseEntity
{
    public Guid TripId { get; set; }
    public int Sequence { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletionSource { get; set; }
    public List<TripStopAction> Actions { get; set; } = new();
}

public class TripStopAction : BaseEntity
{
    public Guid TripStopId { get; set; }
    public Guid SegmentId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public StopActionKind Kind { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletionSource { get; set; }
}

public enum StopActionKind { Pickup, Delivery }
