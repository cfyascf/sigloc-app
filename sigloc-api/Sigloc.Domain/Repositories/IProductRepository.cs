using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, Guid contractorId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalItems)> SearchAsync(
        Guid contractorId,
        string? search,
        ProductCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(Guid contractorId, string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Ids of the route segments (trechos) that reference this product.</summary>
    Task<IReadOnlyList<string>> GetLinkedSegmentIdsAsync(Guid productId, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
}