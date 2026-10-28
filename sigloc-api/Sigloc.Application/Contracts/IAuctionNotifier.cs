namespace Sigloc.Application.Contracts;

/// <summary>
/// Tells the companies involved in an auction what happened to it. Every method is called after the
/// operation committed and never throws: a notification failure is logged, not surfaced.
/// <paramref name="route"/> is the "origin → destination" label shown in the notification text.
/// </summary>
public interface IAuctionNotifier
{
    /// <summary>The contractor's active partner carriers learn about the new auction.</summary>
    Task AuctionOpenedAsync(Guid auctionId, Guid contractorId, string route, CancellationToken cancellationToken = default);

    /// <summary>The contractor learns about the new or updated bid; a carrier that lost the lead learns it was outbid.</summary>
    Task BidPlacedAsync(Guid auctionId, Guid contractorId, Guid carrierId, string route, decimal totalValue, bool updated,
        Guid? outbidCarrierId, CancellationToken cancellationToken = default);

    /// <summary>The winner learns about its trip; the other bidders learn the auction closed.</summary>
    Task AuctionAwardedAsync(Guid tripId, Guid winnerCarrierId, IReadOnlyCollection<Guid> otherBidderIds, string route,
        CancellationToken cancellationToken = default);

    /// <summary>Carriers that had bid learn the contractor cancelled the auction.</summary>
    Task AuctionCancelledAsync(IReadOnlyCollection<Guid> bidderCarrierIds, string route, CancellationToken cancellationToken = default);

    /// <summary>Tells the contractor which bid the automatic award picked.</summary>
    Task AuctionAutoAwardedAsync(Guid contractorId, Guid tripId, string route, decimal totalValue, CancellationToken cancellationToken = default);

    /// <summary>An automatic-award auction ended with no winner: the contractor and the bidders are told why.</summary>
    Task AuctionEndedWithoutWinnerAsync(Guid contractorId, IReadOnlyCollection<Guid> bidderCarrierIds, string route, string reason,
        CancellationToken cancellationToken = default);
}
