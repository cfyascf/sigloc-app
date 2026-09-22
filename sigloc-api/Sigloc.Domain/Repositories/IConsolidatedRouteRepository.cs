using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IConsolidatedRouteRepository
{
    /// <summary>Stages a new consolidated route. Does not persist until the unit of work is saved.</summary>
    Task AddAsync(ConsolidatedRoute route, CancellationToken cancellationToken = default);
}
