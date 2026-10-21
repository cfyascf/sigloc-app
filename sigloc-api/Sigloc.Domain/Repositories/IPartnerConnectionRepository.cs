using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IPartnerConnectionRepository
{
    Task<bool> ExistsAsync(Guid contractorId, Guid carrierId, CancellationToken cancellationToken = default);

    /// <summary>Todas as conexões de um contratante, já com a Carrier carregada (Include).</summary>
    Task<IEnumerable<PartnerConnection>> GetByContractorAsync(Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>Todas as conexões de uma transportadora, já com o Contractor carregado (Include).</summary>
    Task<IEnumerable<PartnerConnection>> GetByCarrierAsync(Guid carrierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a tracked connection by id so its status can be changed in a transaction, or
    /// null when it does not exist.
    /// </summary>
    Task<PartnerConnection?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Stages a new partnership connection. Persisted when the unit of work is committed.</summary>
    Task AddAsync(PartnerConnection connection, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to a tracked connection.</summary>
    Task UpdateAsync(PartnerConnection connection, CancellationToken cancellationToken = default);
}
