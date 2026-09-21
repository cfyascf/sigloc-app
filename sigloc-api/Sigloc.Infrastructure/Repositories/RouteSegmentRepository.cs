using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class RouteSegmentRepository : IRouteSegmentRepository
{
    private readonly SiglocDbContext _dbContext;

    public RouteSegmentRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RouteSegment?> GetByIdAsync(Guid id, Guid contractorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RouteSegments
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id && s.ContractorId == contractorId, cancellationToken);
    }

    public async Task<(IReadOnlyList<RouteSegment> Items, int TotalItems)> SearchAsync(
        Guid contractorId,
        string? origin,
        string? destination,
        SegmentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RouteSegments
            .AsNoTracking()
            .Where(s => s.ContractorId == contractorId);

        if (!string.IsNullOrWhiteSpace(origin))
        {
            var term = origin.Trim().ToLower();
            query = query.Where(s => s.OriginAddress.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(destination))
        {
            var term = destination.Trim().ToLower();
            query = query.Where(s => s.DestinationAddress.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(
        Guid contractorId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<Product>();
        }

        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.ContractorId == contractorId && productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RouteSegment segment, CancellationToken cancellationToken = default)
    {
        await _dbContext.RouteSegments.AddAsync(segment, cancellationToken);
        // The segment and its associative rows are persisted in a single SaveChanges
        // call, which EF Core wraps in one database transaction (atomicity).
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RouteSegment segment, CancellationToken cancellationToken = default)
    {
        // The new item set arrives on segment.Items. Remove any associative rows that
        // are still tracked from the original load, then attach the replacement rows.
        var trackedItems = await _dbContext.ProductRouteSegments
            .Where(i => i.RouteSegmentId == segment.Id)
            .ToListAsync(cancellationToken);

        var replacementItems = segment.Items.ToList();
        segment.Items.Clear();

        _dbContext.ProductRouteSegments.RemoveRange(trackedItems);

        foreach (var item in replacementItems)
        {
            item.Id = Guid.NewGuid();
            item.RouteSegmentId = segment.Id;
            item.Product = null;
            segment.Items.Add(item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(RouteSegment segment, CancellationToken cancellationToken = default)
    {
        // Associative rows are removed by the configured cascade delete.
        _dbContext.RouteSegments.Remove(segment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
