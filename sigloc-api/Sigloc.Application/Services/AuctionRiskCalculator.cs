using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>
/// Computes the on-demand risk indicator for an auction. Severity is evaluated from the
/// most to the least critical so that overlapping conditions resolve to the worst level.
/// </summary>
internal static class AuctionRiskCalculator
{
    public const string Normal = "NORMAL";
    public const string Warning = "WARNING";
    public const string Critical = "CRITICAL";

    /// <summary>
    /// Applies the business rules using the earliest pickup deadline of the route.
    /// </summary>
    /// <param name="earliestPickup">Earliest pickup deadline among the route's segments, or null when unknown.</param>
    /// <param name="expiresAt">Auction closing timestamp.</param>
    /// <param name="status">Current auction status.</param>
    /// <param name="totalBids">Number of bids placed on the auction.</param>
    /// <param name="now">Reference "current" time.</param>
    public static string Evaluate(
        DateTimeOffset? earliestPickup,
        DateTimeOffset expiresAt,
        AuctionStatus status,
        int totalBids,
        DateTimeOffset now)
    {
        var untilExpiry = expiresAt - now;
        var untilPickup = earliestPickup.HasValue ? earliestPickup.Value - now : (TimeSpan?)null;

        // CRITICAL
        var expiryImminentNoBids = untilExpiry < TimeSpan.FromMinutes(30) && totalBids == 0;
        var pickupImminentOpen = untilPickup.HasValue
            && untilPickup.Value < TimeSpan.FromHours(6)
            && status == AuctionStatus.Open;
        if (expiryImminentNoBids || pickupImminentOpen)
        {
            return Critical;
        }

        // WARNING
        var pickupSoon = untilPickup.HasValue && untilPickup.Value < TimeSpan.FromHours(12);
        var expirySoon = untilExpiry < TimeSpan.FromHours(2);
        if (pickupSoon || expirySoon)
        {
            return Warning;
        }

        // NORMAL (explicit rule) and safe default.
        return Normal;
    }
}
