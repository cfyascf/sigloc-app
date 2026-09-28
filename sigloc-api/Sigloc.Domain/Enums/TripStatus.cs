namespace Sigloc.Domain.Enums;

/// <summary>
/// Lifecycle state of a physical trip (Viagem) created when an auction is awarded.
/// </summary>
public enum TripStatus
{
    AwaitingPickup,
    InTransit,
    Delivered,
    Cancelled
}
