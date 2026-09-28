using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface ITripRepository
{
    /// <summary>Stages a new trip. Does not persist until the unit of work is saved.</summary>
    Task AddAsync(Trip trip, CancellationToken cancellationToken = default);
}
