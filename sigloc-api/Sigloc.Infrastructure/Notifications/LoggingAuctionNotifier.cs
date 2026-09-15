using Microsoft.Extensions.Logging;
using Sigloc.Application.Contracts;

namespace Sigloc.Infrastructure.Notifications;

/// <summary>
/// No-op notifier that logs the "auction opened" event. Acts as the extension point for
/// a future real-time transport (WebSocket/SignalR) that refreshes the partner carriers'
/// freight board.
/// </summary>
public class LoggingAuctionNotifier : IAuctionNotifier
{
    private readonly ILogger<LoggingAuctionNotifier> _logger;

    public LoggingAuctionNotifier(ILogger<LoggingAuctionNotifier> logger)
    {
        _logger = logger;
    }

    public Task AuctionOpenedAsync(Guid auctionId, Guid routeId, Guid contractorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Auction {AuctionId} opened for route {RouteId} (contractor {ContractorId}).",
            auctionId, routeId, contractorId);

        return Task.CompletedTask;
    }
}
