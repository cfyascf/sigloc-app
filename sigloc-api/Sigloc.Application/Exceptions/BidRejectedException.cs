namespace Sigloc.Application.Exceptions;

/// <summary>
/// Machine-readable code describing which Constraint Engine trava rejected the bid.
/// Consumed by the API layer to build a stable error payload for the UI toast.
/// </summary>
public enum BidRejectionCode
{
    /// <summary>The offered value is above the auction ceiling (teto).</summary>
    AboveCeiling,

    /// <summary>The offered value is below the ANTT minimum freight floor for the vehicle.</summary>
    BelowAnttFloor,

    /// <summary>The offered value is not a positive amount.</summary>
    InvalidValue,

    /// <summary>The consolidated route weight exceeds the vehicle capacity.</summary>
    Overweight,

    /// <summary>The consolidated route volume exceeds the vehicle capacity.</summary>
    Overvolume,

    /// <summary>The vehicle does not meet the cargo equipment/compliance requirement.</summary>
    EquipmentMismatch,

    /// <summary>The vehicle already has an active trip overlapping the route window.</summary>
    Overbooked
}

/// <summary>
/// Raised when the Constraint Engine (Hard Block) rejects a carrier bid before it is
/// persisted. Produces a 400 response carrying the specific rejection reason so the UI
/// can show the exact motive (e.g. "Excesso de 1200kg").
/// </summary>
public sealed class BidRejectedException : Exception
{
    public BidRejectionCode Code { get; }

    public BidRejectedException(BidRejectionCode code, string message)
        : base(message)
    {
        Code = code;
    }
}
