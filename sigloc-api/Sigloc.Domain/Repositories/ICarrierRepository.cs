using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface ICarrierRepository
{
    Task<Carrier?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>Loads a carrier by id (read-only).</summary>
    Task<Carrier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked carrier by id so its editable data can be updated in a transaction.</summary>
    Task<Carrier?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> CnpjExistsAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>Stages a new carrier. Persisted when the unit of work is committed.</summary>
    Task AddAsync(Carrier carrier, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to a tracked carrier.</summary>
    Task UpdateAsync(Carrier carrier, CancellationToken cancellationToken = default);
}
