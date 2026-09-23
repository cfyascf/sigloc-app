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
        await _dbContext.ConsolidatedRoutes.AddAsync(route, cancellationToken);
    }
}
