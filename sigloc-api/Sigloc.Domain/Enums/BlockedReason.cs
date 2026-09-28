namespace Sigloc.Domain.Enums;

/// <summary>
/// Reason a bid/allocation attempt was blocked by the anti-overbooking engine, used to
/// classify entries in the <c>BlockedBidAttempt</c> log that feeds the executive dashboard.
/// </summary>
public enum BlockedReason
{
    /// <summary>Attempt rejected because the pickup/delivery SLA could not be met.</summary>
    Sla,

    /// <summary>Attempt rejected because the vehicle volume capacity was exceeded.</summary>
    Volume,

    /// <summary>Attempt rejected because the vehicle weight capacity was exceeded.</summary>
    Weight,

    /// <summary>Attempt rejected because the vehicle did not meet the cargo equipment/compliance requirement.</summary>
    Equipment
}
