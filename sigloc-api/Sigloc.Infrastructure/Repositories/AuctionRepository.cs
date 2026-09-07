using Microsoft.EntityFrameworkCore;
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

    public async Task<Auction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Auctions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Auctions
            .AsNoTracking() 
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Auctions.AddAsync(auction, cancellationToken);
    }

    public async Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        _dbContext.Auctions.Update(auction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        _dbContext.Auctions.Remove(auction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}