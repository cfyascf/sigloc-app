using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly SiglocDbContext _dbContext;

    public TripRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        // Staged only; the caller commits through the unit of work transaction.
        await _dbContext.Trips.AddAsync(trip, cancellationToken);
    }

    public async Task<IReadOnlyList<TripScheduleWindow>> GetActiveWindowsForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        // Routes still occupied by the vehicle: trips that are not delivered nor cancelled.
        var windows = await (
            from trip in _dbContext.Trips.AsNoTracking()
            where trip.VehicleId == vehicleId
                && trip.Status != TripStatus.Delivered
                && trip.Status != TripStatus.Cancelled
            join segment in _dbContext.RouteSegments.AsNoTracking()
                on trip.RouteId equals segment.RouteId
            group segment by trip.RouteId into g
            select new TripScheduleWindow(
                g.Min(s => s.PickupDeadline),
                g.Max(s => s.DeliveryDeadline)))
            .ToListAsync(cancellationToken);

        return windows;
    }
}
