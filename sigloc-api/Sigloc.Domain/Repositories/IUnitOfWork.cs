namespace Sigloc.Domain.Repositories;

/// <summary>
/// Commits all pending changes staged through the repositories as a single
/// atomic unit of work.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a single database transaction. Every
    /// change staged during the operation is committed together, or fully rolled back
    /// if the operation throws. Safe to use with connection-resiliency retry strategies.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
