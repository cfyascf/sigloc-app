using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

/// <summary>SLA window [first pickup, last delivery] of a route occupied by an active trip.</summary>
public sealed record TripScheduleWindow(DateTimeOffset Start, DateTimeOffset End);

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
}
