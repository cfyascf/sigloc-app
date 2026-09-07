using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IConsolidatedRouteRepository
{
    Task<ConsolidatedRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConsolidatedRoute>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ConsolidatedRoute consolidatedRoute, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConsolidatedRoute consolidatedRoute, CancellationToken cancellationToken = default);
    Task DeleteAsync(ConsolidatedRoute consolidatedRoute, CancellationToken cancellationToken = default);
}