using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;

namespace Sigloc.Api.BackgroundJobs;

/// <summary>
/// Elects the winner of automatic-award auctions once their end time passes. Runs every
/// <c>AutoAward:IntervalSeconds</c> (60 by default); disabled with <c>AutoAward:Enabled=false</c>.
/// Each auction is handled in its own DI scope (own DbContext), so one failure does not leave
/// tracked changes behind for the next.
/// </summary>
public sealed class AuctionAutoAwardWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AuctionAutoAwardWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("AutoAward:Enabled", true))
        {
            logger.LogInformation("Automatic auction award is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(10, configuration.GetValue("AutoAward:IntervalSeconds", 60)));
        using var timer = new PeriodicTimer(interval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<(Guid AuctionId, Guid ContractorId)> expired;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            expired = await scope.ServiceProvider.GetRequiredService<IAuctionService>()
                .GetExpiredAutomaticAuctionsAsync(now, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not list the expired automatic-award auctions.");
            return;
        }

        foreach (var (auctionId, contractorId) in expired)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var outcome = await scope.ServiceProvider.GetRequiredService<IAuctionService>()
                    .AutoAwardAsync(contractorId, auctionId, now, cancellationToken);
                if (outcome != AutoAwardOutcome.Skipped)
                {
                    logger.LogInformation("Automatic award of auction {AuctionId}: {Outcome}.", auctionId, outcome);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Automatic award of auction {AuctionId} failed; it is retried on the next run.", auctionId);
            }
        }
    }
}
