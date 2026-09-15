namespace Sigloc.Domain.Enums;

/// <summary>
/// Maps between the API wire values and the internal <see cref="RouteStatus"/> and
/// <see cref="AuctionStatus"/> enums. Wire values are kept in English to match the
/// JSON contract while the domain model uses the same English identifiers.
/// </summary>
public static class RouteEnumMappings
{
    private static readonly Dictionary<RouteStatus, string> RouteStatusToWire = new()
    {
        [RouteStatus.Planned] = "PLANNED",
        [RouteStatus.InAuction] = "IN_AUCTION",
        [RouteStatus.InTransit] = "IN_TRANSIT",
        [RouteStatus.Completed] = "COMPLETED"
    };

    private static readonly Dictionary<AuctionStatus, string> AuctionStatusToWire = new()
    {
        [AuctionStatus.Open] = "OPEN",
        [AuctionStatus.Closed] = "CLOSED",
        [AuctionStatus.Cancelled] = "CANCELLED"
    };

    public static string ToWire(this RouteStatus value) => RouteStatusToWire[value];

    public static string ToWire(this AuctionStatus value) => AuctionStatusToWire[value];
}
