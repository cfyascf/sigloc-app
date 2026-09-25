using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IAuctionRepository
{
    /// <summary>Stages a new auction. Does not persist until the unit of work is saved.</summary>
    Task AddAsync(Auction auction, CancellationToken cancellationToken = default);
}
