namespace Sigloc.Domain.Enums;

/// <summary>
/// Lifecycle state of a route segment (Trecho).
/// </summary>
public enum SegmentStatus
{
    /// <summary>Not yet offered in an auction; editable and selectable for a new route.</summary>
    Available,

    /// <summary>Consolidated into a route that is currently offered in an auction ("Em leilão").</summary>
    Routed,

    /// <summary>The auction ended and the route became an active trip that is underway ("Em execução").</summary>
    InExecution,

    /// <summary>The active route the segment belongs to has been completed.</summary>
    Completed
}
