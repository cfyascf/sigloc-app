using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class BidRepository : IBidRepository
{
    private readonly SiglocDbContext _dbContext;

    public BidRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Bid bid, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.Bids.AddAsync(bid, cancellationToken);
    }

    public async Task<Bid?> GetTrackedByCarrierAndAuctionAsync(
        Guid carrierId,
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bids
            .Where(b => b.CarrierId == carrierId
                && b.AuctionId == auctionId
                && b.Status != BidStatus.Withdrawn)
            .OrderByDescending(b => b.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Bid?> GetByCarrierAndAuctionAsync(
        Guid carrierId,
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bids
            .AsNoTracking()
            .Where(b => b.CarrierId == carrierId
                && b.AuctionId == auctionId
                && b.Status != BidStatus.Withdrawn)
            .OrderByDescending(b => b.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuctionBidMetrics>> GetMetricsForAuctionsAsync(
        IReadOnlyCollection<Guid> auctionIds,
        CancellationToken cancellationToken = default)
    {
        if (auctionIds.Count == 0)
        {
            return Array.Empty<AuctionBidMetrics>();
        }

        // Withdrawn bids do not count towards the best price or the total.
        var metrics = await _dbContext.Bids
            .AsNoTracking()
            .Where(b => auctionIds.Contains(b.AuctionId) && b.Status != BidStatus.Withdrawn)
            .GroupBy(b => b.AuctionId)
            .Select(g => new AuctionBidMetrics(
                g.Key,
                g.Min(b => b.TotalValue),
                g.Count()))
            .ToListAsync(cancellationToken);

        return metrics;
    }

    public async Task<BestBidWithCarrier?> GetBestBidWithCarrierAsync(
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        var best = await (
            from bid in _dbContext.Bids.AsNoTracking()
            where bid.AuctionId == auctionId && bid.Status != BidStatus.Withdrawn
            join carrier in _dbContext.Carriers.AsNoTracking() on bid.CarrierId equals carrier.Id
            orderby bid.TotalValue, bid.SubmittedAt
            select new BestBidWithCarrier(bid.TotalValue, carrier.CompanyName))
            .FirstOrDefaultAsync(cancellationToken);

        return best;
    }

    public async Task<IReadOnlyList<RankedBid>> GetRankedBidsAsync(
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        var ranked = await (
            from bid in _dbContext.Bids.AsNoTracking()
            where bid.AuctionId == auctionId && bid.Status != BidStatus.Withdrawn
            join carrier in _dbContext.Carriers.AsNoTracking() on bid.CarrierId equals carrier.Id
            join vehicle in _dbContext.Vehicles.AsNoTracking() on bid.VehicleId equals vehicle.Id
            orderby bid.TotalValue, carrier.AverageRating descending
            select new RankedBid(bid, carrier, vehicle))
            .ToListAsync(cancellationToken);

        return ranked;
    }

    public async Task<IReadOnlyList<Bid>> GetTrackedByAuctionAsync(
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bids
            .Where(b => b.AuctionId == auctionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, BidStatus>> GetCarrierBidStatusesAsync(
        Guid carrierId,
        IReadOnlyCollection<Guid> auctionIds,
        CancellationToken cancellationToken = default)
    {
        if (auctionIds.Count == 0)
        {
            return new Dictionary<Guid, BidStatus>();
        }

        // A carrier holds a single active bid per auction; if several exist, keep the most
        // relevant one (Winner > Winning > Losing > Pending) via descending enum order.
        var rows = await _dbContext.Bids
            .AsNoTracking()
            .Where(b => b.CarrierId == carrierId
                && auctionIds.Contains(b.AuctionId)
                && b.Status != BidStatus.Withdrawn)
            .Select(b => new { b.AuctionId, b.Status })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.AuctionId)
            .ToDictionary(g => g.Key, g => g.Max(r => r.Status));
    }

    public async Task<BidStatus?> GetCarrierBidStatusAsync(
        Guid carrierId,
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        var statuses = await _dbContext.Bids
            .AsNoTracking()
            .Where(b => b.CarrierId == carrierId
                && b.AuctionId == auctionId
                && b.Status != BidStatus.Withdrawn)
            .Select(b => b.Status)
            .ToListAsync(cancellationToken);

        return statuses.Count == 0 ? null : statuses.Max();
    }
}
