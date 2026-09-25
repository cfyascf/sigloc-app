using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class ConsolidatedRouteRepository : IConsolidatedRouteRepository
{
    private readonly SiglocDbContext _dbContext;

    public ConsolidatedRouteRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ConsolidatedRoute route, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.ConsolidatedRoutes.AddAsync(route, cancellationToken);
    }
}
