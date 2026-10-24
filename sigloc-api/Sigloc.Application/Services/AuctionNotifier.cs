using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

/// <summary>Turns auction events into in-app notifications (see <see cref="NotificationCatalog"/>).</summary>
public sealed class AuctionNotifier(
    INotificationPublisher publisher, IPartnerConnectionRepository partners, IContractorRepository contractors,
    ICarrierRepository carriers, ILogger<AuctionNotifier>? logger = null) : IAuctionNotifier
{
    private readonly ILogger _logger = logger ?? NullLogger<AuctionNotifier>.Instance;

    public Task AuctionOpenedAsync(Guid auctionId, Guid contractorId, string route, CancellationToken cancellationToken = default) =>
        SafelyAsync(nameof(AuctionOpenedAsync), async () =>
        {
            var contractor = await contractors.GetByIdAsync(contractorId, cancellationToken);
            if (contractor is null) return [];
            var name = NotificationCatalog.CompanyName(contractor.CompanyName, contractor.TradeName);
            return (await partners.GetByContractorAsync(contractorId, cancellationToken))
                .Where(c => c.Status == PartnershipStatus.Active)
                .Select(c => c.CarrierId)
                .Distinct()
                .Select(carrierId => NotificationCatalog.AuctionOpened(carrierId, name, route, auctionId))
                .ToList();
        });

    public Task BidPlacedAsync(Guid auctionId, Guid contractorId, Guid carrierId, string route, decimal totalValue, bool updated,
        Guid? outbidCarrierId, CancellationToken cancellationToken = default) =>
        SafelyAsync(nameof(BidPlacedAsync), async () =>
        {
            var carrier = await carriers.GetByIdAsync(carrierId, cancellationToken);
            var name = carrier is null ? "Uma transportadora" : NotificationCatalog.CompanyName(carrier.CompanyName, carrier.TradeName);
            var messages = new List<NotificationMessage> { NotificationCatalog.BidReceived(contractorId, name, route, totalValue, updated, auctionId) };
            if (outbidCarrierId is { } outbid) messages.Add(NotificationCatalog.BidOutbid(outbid, route, auctionId));
            return messages;
        });

    public Task AuctionAwardedAsync(Guid tripId, Guid winnerCarrierId, IReadOnlyCollection<Guid> otherBidderIds, string route,
        CancellationToken cancellationToken = default) =>
        SafelyAsync(nameof(AuctionAwardedAsync), () => Task.FromResult<IReadOnlyCollection<NotificationMessage>>(
            otherBidderIds.Where(id => id != winnerCarrierId).Distinct()
                .Select(id => NotificationCatalog.AuctionLost(id, route))
                .Prepend(NotificationCatalog.AuctionWon(winnerCarrierId, route, tripId))
                .ToList()));

    public Task AuctionCancelledAsync(IReadOnlyCollection<Guid> bidderCarrierIds, string route, CancellationToken cancellationToken = default) =>
        SafelyAsync(nameof(AuctionCancelledAsync), () => Task.FromResult<IReadOnlyCollection<NotificationMessage>>(
            bidderCarrierIds.Distinct().Select(id => NotificationCatalog.AuctionCancelled(id, route)).ToList()));

    // The auction operation already committed: a failed lookup must not turn its success into an error.
    private async Task SafelyAsync(string evt, Func<Task<IReadOnlyCollection<NotificationMessage>>> build)
    {
        IReadOnlyCollection<NotificationMessage> messages;
        try
        {
            messages = await build();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not build the {Event} notifications.", evt);
            return;
        }
        await publisher.PublishAsync(messages);
    }
}
