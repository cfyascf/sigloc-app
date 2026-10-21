using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IPartnerNetworkService
{
    Task<PartnerNetworkDto> GetNetworkAsync(Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>Rede de contratantes conectados a uma transportadora.</summary>
    Task<CarrierNetworkDto> GetCarrierNetworkAsync(Guid carrierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desfaz (encerra) uma parceria sem removê-la: marca a conexão como
    /// <c>Rejected</c> para que permaneça no histórico como inativa. Pode ser acionada por
    /// qualquer um dos lados; <paramref name="companyId"/> deve ser o contratante ou a
    /// transportadora da conexão.
    /// </summary>
    Task RemovePartnershipAsync(Guid companyId, Guid connectionId, CancellationToken cancellationToken = default);
}