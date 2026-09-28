using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IPartnerNetworkService
{
    Task<PartnerNetworkDto> GetNetworkAsync(Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>Rede de contratantes conectados a uma transportadora.</summary>
    Task<CarrierNetworkDto> GetCarrierNetworkAsync(Guid carrierId, CancellationToken cancellationToken = default);
}