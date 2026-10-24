using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface ITripProofService
{
    /// <summary>Opens a proof-of-delivery file of a trip visible to <paramref name="scope"/>.</summary>
    Task<ProofOfDeliveryFileDto> OpenAsync(TripScope scope, Guid tripId, Guid proofId, CancellationToken ct = default);
}
