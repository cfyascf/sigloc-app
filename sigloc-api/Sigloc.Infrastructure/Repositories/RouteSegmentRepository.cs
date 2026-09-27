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

    public async Task<IReadOnlyList<RouteSegment>> GetByIdsAsync(
        Guid contractorId,
        IReadOnlyCollection<Guid> ids,
        bool tracked,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<RouteSegment>();
        }

        var query = _dbContext.RouteSegments
            .Where(s => s.ContractorId == contractorId && ids.Contains(s.Id))
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RouteSegment segment, CancellationToken cancellationToken = default)
    {
        await _dbContext.RouteSegments.AddAsync(segment, cancellationToken);
        // The segment and its associative rows are persisted in a single SaveChanges
        // call, which EF Core wraps in one database transaction (atomicity).
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        RouteSegment segment,
        IReadOnlyCollection<ProductRouteSegment> items,
        CancellationToken cancellationToken = default)
    {
        // The execution strategy owns the retry loop; the transaction is opened inside it so a
        // retry replays the whole operation atomically. Opening a transaction directly is not
        // allowed while a retrying execution strategy (EnableRetryOnFailure) is configured.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Wrap the deterministic delete + insert/update in a single transaction so the
            // operation stays atomic: ExecuteDelete runs immediately against the database, and
            // the subsequent SaveChanges must commit (or roll back) together with it.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Delete the existing associative rows directly against the database so we do not
            // rely on reassigning/clearing the tracked navigation collection (which triggers
            // EF Core orphan fix-up and produces conflicting modification commands that surface
            // as a DbUpdateConcurrencyException). This also detaches any of those rows that were
            // loaded via Include so they are not tracked while we add the replacements.
            await _dbContext.ProductRouteSegments
                .Where(i => i.RouteSegmentId == segment.Id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var entry in _dbContext.ChangeTracker
                .Entries<ProductRouteSegment>()
                .Where(e => e.Entity.RouteSegmentId == segment.Id)
                .ToList())
            {
                entry.State = EntityState.Detached;
            }

            var replacements = items.Select(item => new ProductRouteSegment
            {
                Id = Guid.NewGuid(),
                RouteSegmentId = segment.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList();

            await _dbContext.ProductRouteSegments.AddRangeAsync(replacements, cancellationToken);

            // segment is tracked; its scalar changes are persisted together with the new rows.
            // The RouteSegment UPDATE matches its own row, so exactly one row is affected.
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task DeleteAsync(RouteSegment segment, CancellationToken cancellationToken = default)
    {
        // Associative rows are removed by the configured cascade delete.
        _dbContext.RouteSegments.Remove(segment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
