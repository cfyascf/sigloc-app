using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IPartnerNetworkService
{
    Task<PartnerNetworkDto> GetNetworkAsync(Guid contractorId, CancellationToken cancellationToken = default);
}