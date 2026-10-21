using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IContractorRepository
{
    Task<Contractor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked contractor by id so its editable data can be updated in a transaction.</summary>
    Task<Contractor?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> CnpjExistsAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>Stages a new contractor. Persisted when the unit of work is committed.</summary>
    Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to a tracked contractor.</summary>
    Task UpdateAsync(Contractor contractor, CancellationToken cancellationToken = default);
}
