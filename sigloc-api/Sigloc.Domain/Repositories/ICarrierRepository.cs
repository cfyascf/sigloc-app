using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface ICarrierRepository
{
    Task<Carrier?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);

    Task<bool> CnpjExistsAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>Stages a new carrier. Persisted when the unit of work is committed.</summary>
    Task AddAsync(Carrier carrier, CancellationToken cancellationToken = default);
}
