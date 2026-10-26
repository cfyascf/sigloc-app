using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>
/// Computes the on-demand risk indicator for a route segment. A segment only carries risk while it
/// is still <see cref="SegmentStatus.Available"/> (not yet offered into an auction): the closer its
/// pickup deadline gets without being offered, the higher the risk. Segments already routed, in
/// transit or completed are always <see cref="Normal"/>.
/// </summary>
internal static class RouteSegmentRiskCalculator
{
    public const string Normal = "NORMAL";
    public const string Warning = "WARNING";
    // Same code the auction/trip risk and the UI use (sigloc-ui/src/constants/risk.js).
    public const string Critical = "CRITIC";

    // Pickup is imminent (or overdue): high risk when the segment still hasn't been offered.
    private static readonly TimeSpan CriticalWindow = TimeSpan.FromHours(6);

    // Pickup is approaching: moderate risk.
    private static readonly TimeSpan WarningWindow = TimeSpan.FromHours(12);

    /// <summary>
    /// Evaluates the risk from how close the pickup deadline is for an available segment.
    /// </summary>
    /// <param name="pickupDeadline">The segment's pickup deadline.</param>
    /// <param name="status">Current segment status.</param>
    /// <param name="now">Reference "current" time.</param>
    public static string Evaluate(DateTimeOffset pickupDeadline, SegmentStatus status, DateTimeOffset now)
    {
        // Once offered/routed the segment is handled elsewhere, so pickup proximity no longer implies risk.
        if (status != SegmentStatus.Available)
        {
            return Normal;
        }

        var untilPickup = pickupDeadline - now;

        if (untilPickup < CriticalWindow)
        {
            return Critical;
        }

        if (untilPickup < WarningWindow)
        {
            return Warning;
        }

        return Normal;
    }
}
