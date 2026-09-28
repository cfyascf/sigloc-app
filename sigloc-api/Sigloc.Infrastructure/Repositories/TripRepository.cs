using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly SiglocDbContext _dbContext;

    public TripRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.Trips.AddAsync(trip, cancellationToken);
    }
}
