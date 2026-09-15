using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly SiglocDbContext _dbContext;

    public AuctionRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.Auctions.AddAsync(auction, cancellationToken);
    }
}
