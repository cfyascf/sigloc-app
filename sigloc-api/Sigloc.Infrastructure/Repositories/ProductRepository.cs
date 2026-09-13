using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SiglocDbContext _dbContext;

    public ProductRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetByIdAsync(Guid id, Guid contractorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.ContractorId == contractorId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalItems)> SearchAsync(
        Guid contractorId,
        string? search,
        ProductCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(p => p.ContractorId == contractorId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term));
        }

        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<bool> SkuExistsAsync(Guid contractorId, string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                p => p.ContractorId == contractorId
                    && p.Sku == sku
                    && (excludeId == null || p.Id != excludeId),
                cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetLinkedSegmentIdsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Route segments (trechos) are not modelled yet; no product can be in use.
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}