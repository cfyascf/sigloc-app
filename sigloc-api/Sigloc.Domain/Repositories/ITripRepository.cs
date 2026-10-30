using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

/// <summary>SLA window [first pickup, last delivery] of a route occupied by an active trip.</summary>
public sealed record TripScheduleWindow(DateTimeOffset Start, DateTimeOffset End);

/// <summary>
/// Where and when a vehicle finishes a delivery of one of its trips: the segment delivery
/// deadline, its destination coordinate ("longitude,latitude") and the place shown to users.
/// </summary>
public sealed record VehicleDelivery(DateTimeOffset DeliveryDeadline, string DestinationCoordinate, string DestinationPlace);

public interface ITripRepository
{
    /// <summary>Stages a new trip. Does not persist until the unit of work is saved.</summary>
    Task AddAsync(Trip trip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the SLA windows of the routes currently occupied by the vehicle's active
    /// trips (not yet delivered or cancelled). Each window is derived from the earliest
    /// pickup and the latest delivery deadline of the route's segments, and is used by the
    /// anti-overbooking trava to detect chronological overlaps.
    /// </summary>
    Task<IReadOnlyList<TripScheduleWindow>> GetActiveWindowsForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the latest delivery the vehicle is scheduled to make up to <paramref name="until"/>
    /// on any of its trips that was not cancelled, i.e. where the vehicle will be before
    /// starting a route that picks up at <paramref name="until"/>. Null when there is none.
    /// </summary>
    Task<VehicleDelivery?> GetLastDeliveryUntilAsync(
        Guid vehicleId,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of trips awaiting pickup or in transit between the contractor and each of the
    /// given carriers. Carriers without active trips are absent from the result.
    /// </summary>
    Task<Dictionary<Guid, int>> CountActiveByCarrierForContractorAsync(
        Guid contractorId,
        IReadOnlyCollection<Guid> carrierIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of distinct origin → destination lanes (segment addresses) the carrier is running
    /// for each contractor on trips awaiting pickup or in transit. Contractors without any are
    /// absent from the result.
    /// </summary>
    Task<Dictionary<Guid, int>> CountActiveLanesByContractorForCarrierAsync(
        Guid carrierId,
        IReadOnlyCollection<Guid> contractorIds,
        CancellationToken cancellationToken = default);
}
