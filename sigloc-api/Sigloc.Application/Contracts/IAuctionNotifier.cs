namespace Sigloc.Application.Contracts;

/// <summary>
/// Dispatches a "new auction opened" event so partner carriers' freight boards can be
/// refreshed. This is an extension point: the current implementation only logs the
/// event, but it is where a real-time transport (WebSocket/SignalR) would be plugged in.
/// </summary>
public interface IAuctionNotifier
{
    Task AuctionOpenedAsync(Guid auctionId, Guid routeId, Guid contractorId, CancellationToken cancellationToken = default);
}
