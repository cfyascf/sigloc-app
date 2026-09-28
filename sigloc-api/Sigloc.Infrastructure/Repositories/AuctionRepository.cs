using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly SiglocDbContext _dbContext;

    public AuctionRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.Auctions.AddAsync(auction, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuctionWithRoute> Items, int TotalItems)> SearchAsync(
        Guid contractorId,
        string? search,
        AuctionStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Auctions joined to their consolidated route, scoped to the contractor.
        var baseQuery =
            from auction in _dbContext.Auctions.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking()
                on auction.RouteId equals route.Id
            where route.ContractorId == contractorId && auction.Status == status
            select new { auction, route };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            // Filter by auction id (partial text) or by any linked segment's origin/destination.
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.auction.Id.ToString().ToLower(), $"%{term}%")
                || _dbContext.RouteSegments.Any(s =>
                    s.RouteId == x.route.Id
                    && (s.OriginAddress.ToLower().Contains(term)
                        || s.DestinationAddress.ToLower().Contains(term))));
        }

        var totalItems = await baseQuery.CountAsync(cancellationToken);

        var pageRows = await baseQuery
            .OrderBy(x => x.auction.ExpiresAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var routeIds = pageRows.Select(r => r.route.Id).ToList();

        var segments = await _dbContext.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);

        var segmentsByRoute = segments
            .GroupBy(s => s.RouteId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<RouteSegment>)g.ToList());

        var items = pageRows
            .Select(r => new AuctionWithRoute(
                r.auction,
                r.route,
                segmentsByRoute.TryGetValue(r.route.Id, out var list) ? list : Array.Empty<RouteSegment>()))
            .ToList();

        return (items, totalItems);
    }

    public async Task<AuctionWithRoute?> GetDetailAsync(
        Guid id,
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from auction in _dbContext.Auctions.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking()
                on auction.RouteId equals route.Id
            where auction.Id == id && route.ContractorId == contractorId
            select new { auction, route })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var segments = await _dbContext.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId == row.route.Id)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);

        return new AuctionWithRoute(row.auction, row.route, segments);
    }

    public async Task<AuctionWithRoute?> GetForCarrierAnalysisAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from auction in _dbContext.Auctions.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking()
                on auction.RouteId equals route.Id
            where auction.Id == id
            select new { auction, route })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var segments = await _dbContext.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId == row.route.Id)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);

        return new AuctionWithRoute(row.auction, row.route, segments);
    }

    public async Task<Auction?> GetTrackedByIdAsync(
        Guid id,
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        // Tracked query scoped to the contractor via the consolidated route.
        return await (
            from auction in _dbContext.Auctions
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking()
                on auction.RouteId equals route.Id
            where auction.Id == id && route.ContractorId == contractorId
            select auction)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<CarrierAuctionWithRoute> Items, int TotalItems)> SearchAvailableForCarrierAsync(
        Guid carrierId,
        string? search,
        DateTimeOffset now,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // B2B filter (Networking Restrito): only auctions whose contractor has an ACTIVE
        // partnership with the carrier. Only open, non-expired auctions are eligible.
        var baseQuery =
            from auction in _dbContext.Auctions.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking()
                on auction.RouteId equals route.Id
            join partnership in _dbContext.PartnerConnections.AsNoTracking()
                on route.ContractorId equals partnership.ContractorId
            join contractor in _dbContext.Contractors.AsNoTracking()
                on route.ContractorId equals contractor.Id
            where partnership.CarrierId == carrierId
                && partnership.Status == PartnershipStatus.Active
                && auction.Status == AuctionStatus.Open
                && auction.ExpiresAt > now
            select new { auction, route, contractor };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            baseQuery = baseQuery.Where(x =>
                x.contractor.CompanyName.ToLower().Contains(term)
                || (x.contractor.TradeName != null && x.contractor.TradeName.ToLower().Contains(term))
                || _dbContext.RouteSegments.Any(s =>
                    s.RouteId == x.route.Id
                    && (s.OriginAddress.ToLower().Contains(term)
                        || s.DestinationAddress.ToLower().Contains(term))));
        }

        var totalItems = await baseQuery.CountAsync(cancellationToken);

        var pageRows = await baseQuery
            .OrderBy(x => x.auction.ExpiresAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var routeIds = pageRows.Select(r => r.route.Id).ToList();

        var segments = await _dbContext.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId.Value))
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);

        var segmentsByRoute = segments
            .GroupBy(s => s.RouteId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<RouteSegment>)g.ToList());

        var items = pageRows
            .Select(r => new CarrierAuctionWithRoute(
                r.auction,
                r.route,
                segmentsByRoute.TryGetValue(r.route.Id, out var list) ? list : Array.Empty<RouteSegment>(),
                r.contractor.TradeName ?? r.contractor.CompanyName))
            .ToList();

        return (items, totalItems);
    }

    public async Task<CarrierAuctionWithRoute?> GetAvailableForCarrierAsync(
        Guid auctionId,
        Guid carrierId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from auction in _dbContext.Auctions.AsNoTracking()
            join route in _dbContext.ConsolidatedRoutes.AsNoTracking()
                on auction.RouteId equals route.Id
            join partnership in _dbContext.PartnerConnections.AsNoTracking()
                on route.ContractorId equals partnership.ContractorId
            join contractor in _dbContext.Contractors.AsNoTracking()
                on route.ContractorId equals contractor.Id
            where auction.Id == auctionId
                && partnership.CarrierId == carrierId
                && partnership.Status == PartnershipStatus.Active
                && auction.Status == AuctionStatus.Open
                && auction.ExpiresAt > now
            select new { auction, route, contractor })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var segments = await _dbContext.RouteSegments
            .AsNoTracking()
            .Where(s => s.RouteId == row.route.Id)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);

        return new CarrierAuctionWithRoute(
            row.auction,
            row.route,
            segments,
            row.contractor.TradeName ?? row.contractor.CompanyName);
    }

    public async Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        _dbContext.Auctions.Update(auction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        _dbContext.Auctions.Remove(auction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
