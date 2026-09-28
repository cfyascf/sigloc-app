using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class BlockedBidAttemptRepository : IBlockedBidAttemptRepository
{
    private readonly SiglocDbContext _dbContext;

    public BlockedBidAttemptRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BlockedBidAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _dbContext.BlockedBidAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
