using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Auction consolidatedRoute, CancellationToken cancellationToken = default);
    Task UpdateAsync(Auction consolidatedRoute, CancellationToken cancellationToken = default);
    Task DeleteAsync(Auction consolidatedRoute, CancellationToken cancellationToken = default);
}