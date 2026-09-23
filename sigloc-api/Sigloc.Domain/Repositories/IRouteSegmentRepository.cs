using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Repositories;

public interface IRouteSegmentRepository
{
    /// <summary>Loads a segment (scoped to the contractor) including its items and their products.</summary>
    Task<RouteSegment?> GetByIdAsync(Guid id, Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the segments referenced by <paramref name="ids"/> (scoped to the contractor),
    /// including their items and products. Segments that do not exist or belong to another
    /// contractor are simply absent from the result — callers must check the count.
    /// Pass <paramref name="asTracking"/> true when the caller intends to mutate and save
    /// the returned entities (e.g. consolidating them into a route).
    /// </summary>
    Task<IReadOnlyList<RouteSegment>> GetByIdsAsync(
        Guid contractorId,
        IReadOnlyCollection<Guid> ids,
        bool asTracking,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RouteSegment> Items, int TotalItems)> SearchAsync(
        Guid contractorId,
        string? origin,
        string? destination,
        SegmentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the products referenced by the given ids, scoped to the contractor.</summary>
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(
        Guid contractorId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(RouteSegment segment, CancellationToken cancellationToken = default);

    /// <summary>Replaces the items of an existing segment and persists the updated segment.</summary>
    Task UpdateAsync(RouteSegment segment, CancellationToken cancellationToken = default);

    Task DeleteAsync(RouteSegment segment, CancellationToken cancellationToken = default);
}
