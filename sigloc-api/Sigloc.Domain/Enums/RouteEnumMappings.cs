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
        [RouteStatus.AwaitingPickup] = "AWAITING_PICKUP",
        [RouteStatus.InTransit] = "IN_TRANSIT",
        [RouteStatus.Completed] = "COMPLETED"
    };

    private static readonly Dictionary<AuctionStatus, string> AuctionStatusToWire = new()
    {
        [AuctionStatus.Open] = "OPEN",
        [AuctionStatus.Closed] = "CLOSED",
        [AuctionStatus.Cancelled] = "CANCELLED"
    };

    private static readonly Dictionary<BidStatus, string> BidStatusToWire = new()
    {
        [BidStatus.Pending] = "PENDING",
        [BidStatus.Winning] = "WINNING",
        [BidStatus.Losing] = "LOSING",
        [BidStatus.Winner] = "WINNER",
        [BidStatus.Withdrawn] = "WITHDRAWN"
    };

    public static string ToWire(this RouteStatus value) => RouteStatusToWire[value];

    public static string ToWire(this AuctionStatus value) => AuctionStatusToWire[value];

    public static string ToWire(this BidStatus value) => BidStatusToWire[value];
}
