using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IConsolidatedRouteRepository
{
    /// <summary>
    /// Stages the route for insertion. Does NOT call SaveChanges: it is meant to be
    /// combined with other staged changes (e.g. updated route segments, a new auction)
    /// and committed together through <see cref="IUnitOfWork"/> as a single atomic
    /// transaction.
    /// </summary>
    Task AddAsync(ConsolidatedRoute route, CancellationToken cancellationToken = default);
}
