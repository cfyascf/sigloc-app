namespace Sigloc.Domain.Enums;

/// <summary>
/// Lifecycle state of a route segment (Trecho).
/// </summary>
public enum SegmentStatus
{
    Available,
    Routed,
    InTransit,
    Completed
}
