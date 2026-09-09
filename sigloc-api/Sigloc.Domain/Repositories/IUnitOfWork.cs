namespace Sigloc.Domain.Repositories;

/// <summary>
/// Commits all pending changes staged through the repositories as a single
/// atomic unit of work.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
