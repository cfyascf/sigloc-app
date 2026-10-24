using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;

namespace Sigloc.Application.Services;

/// <summary>
/// Opens a trip's proof-of-delivery file for either party. The scope decides who may read it:
/// the carrier that runs the trip or the shipper (contractor) that owns its route.
/// </summary>
public sealed class TripProofService(ITripMonitoringStore store, IFileStorage files) : ITripProofService
{
    public Task<ProofOfDeliveryFileDto> OpenAsync(TripScope scope, Guid tripId, Guid proofId, CancellationToken ct = default) =>
        OpenAsync(store, files, scope, tripId, proofId, ct);

    internal static async Task<ProofOfDeliveryFileDto> OpenAsync(
        ITripMonitoringStore store, IFileStorage files, TripScope scope, Guid tripId, Guid proofId, CancellationToken ct)
    {
        var state = await store.ReadAsync(scope, tripId, ct) ?? throw new TripNotFoundException(tripId);
        var proof = state.Proofs.FirstOrDefault(p => p.Id == proofId) ?? throw new ProofOfDeliveryNotFoundException(proofId);
        var content = await files.OpenReadAsync(proof.StorageKey, ct) ?? throw new ProofOfDeliveryNotFoundException(proofId);
        return new ProofOfDeliveryFileDto(content, proof.ContentType, proof.FileName);
    }
}
