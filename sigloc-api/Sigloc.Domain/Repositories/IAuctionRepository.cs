using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IAuctionRepository
{
    /// <summary>
    /// Stages the auction for insertion. Does NOT call SaveChanges — see
    /// <see cref="IConsolidatedRouteRepository.AddAsync"/> for why.
    /// </summary>
    Task AddAsync(Auction auction, CancellationToken cancellationToken = default);
}
