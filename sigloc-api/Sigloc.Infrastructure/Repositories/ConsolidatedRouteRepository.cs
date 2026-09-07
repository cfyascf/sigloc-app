using Microsoft.EntityFrameworkCore;
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

    public async Task<ConsolidatedRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConsolidatedRoutes
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ConsolidatedRoute>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConsolidatedRoutes
            .AsNoTracking() 
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ConsolidatedRoute consolidatedRoute, CancellationToken cancellationToken = default)
    {
        await _dbContext.ConsolidatedRoutes.AddAsync(consolidatedRoute, cancellationToken);
    }

    public async Task UpdateAsync(ConsolidatedRoute consolidatedRoute, CancellationToken cancellationToken = default)
    {
        _dbContext.ConsolidatedRoutes.Update(consolidatedRoute);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ConsolidatedRoute consolidatedRoute, CancellationToken cancellationToken = default)
    {
        _dbContext.ConsolidatedRoutes.Remove(consolidatedRoute);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}