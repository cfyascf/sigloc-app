using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IContractorRepository
{
    Task<Contractor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> CnpjExistsAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>Stages a new contractor. Persisted when the unit of work is committed.</summary>
    Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default);
}
