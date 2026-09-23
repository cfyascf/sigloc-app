namespace Sigloc.Domain.Enums;

/// <summary>
/// Maps between the API wire values (as specified for the Rota/Leilão contract, in
/// Portuguese) and the internal <see cref="ConsolidatedRouteStatus"/> / <see cref="AuctionStatus"/> enums.
/// </summary>
public static class AuctionEnumMappings
{
    private static readonly Dictionary<ConsolidatedRouteStatus, string> RouteStatusToWire = new()
    {
        [ConsolidatedRouteStatus.Planned] = "PLANEJADA",
        [ConsolidatedRouteStatus.InAuction] = "EM_LEILAO",
        [ConsolidatedRouteStatus.InTransit] = "EM_VIAGEM",
        [ConsolidatedRouteStatus.Completed] = "CONCLUIDA"
    };

    private static readonly Dictionary<AuctionStatus, string> AuctionStatusToWire = new()
    {
        [AuctionStatus.Open] = "ABERTO",
        [AuctionStatus.Closed] = "FECHADO",
        [AuctionStatus.Cancelled] = "CANCELADO"
    };

    public static string ToWire(this ConsolidatedRouteStatus value) => RouteStatusToWire[value];

    public static string ToWire(this AuctionStatus value) => AuctionStatusToWire[value];
}
