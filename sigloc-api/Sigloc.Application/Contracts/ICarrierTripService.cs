using Sigloc.Application.DTOs;
namespace Sigloc.Application.Contracts;

/// <summary>Trip operations for the carrier that was awarded the trip.</summary>
public interface ICarrierTripService
{
    Task<PagedTripsDto> SearchAsync(Guid carrierId, TripQueryDto query, CancellationToken ct = default);

    /// <summary>Finished trips (delivered / cancelled) of the carrier, newest first.</summary>
    Task<PagedTripsDto> SearchHistoryAsync(Guid carrierId, TripQueryDto query, CancellationToken ct = default);

    /// <summary>Detail view; <paramref name="refresh"/> runs the GPS ping/recalculation pipeline.</summary>
    Task<CarrierTripDetailDto> GetDetailAsync(Guid carrierId, Guid tripId, bool refresh = false, CancellationToken ct = default);

    Task<CarrierTripDetailDto> ReportOccurrenceAsync(Guid carrierId, Guid userId, Guid tripId,
        ReportTripOccurrenceRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Confirms the first pending stop. A proof of delivery is required when the stop has a pending delivery.
    /// </summary>
    Task<CarrierTripDetailDto> CompleteStopAsync(Guid carrierId, Guid userId, Guid tripId, Guid stopId,
        UploadedFileDto? proof, string? note, CancellationToken ct = default);

    Task<ProofOfDeliveryFileDto> OpenProofAsync(Guid carrierId, Guid tripId, Guid proofId, CancellationToken ct = default);
}
