using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IPartnershipInviteRepository
{
    Task<PartnershipInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Stages a new invite. Persisted when the unit of work is committed.</summary>
    Task AddAsync(PartnershipInvite invite, CancellationToken cancellationToken = default);
}
