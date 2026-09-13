using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IPartnerConnectionRepository
{
    Task<bool> ExistsAsync(Guid contractorId, Guid carrierId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new partnership connection. Persisted when the unit of work is committed.</summary>
    Task AddAsync(PartnerConnection connection, CancellationToken cancellationToken = default);
}
