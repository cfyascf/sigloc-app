using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IBlockedBidAttemptRepository
{
    /// <summary>
    /// Stages a blocked bid attempt audit entry. Persisted when the caller saves the unit
    /// of work (or immediately when saved directly), feeding the executive dashboard KPI.
    /// </summary>
    Task AddAsync(BlockedBidAttempt attempt, CancellationToken cancellationToken = default);
}
