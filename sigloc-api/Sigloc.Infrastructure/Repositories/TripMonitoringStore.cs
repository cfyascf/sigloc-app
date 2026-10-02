using Microsoft.EntityFrameworkCore;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public sealed class TripMonitoringStore(IDbContextFactory<SiglocDbContext> factory) : ITripMonitoringStore
{
    public async Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = from t in db.Trips.AsNoTracking()
                   join r in db.ConsolidatedRoutes.AsNoTracking() on t.RouteId equals r.Id
                   join c in db.Carriers.AsNoTracking() on t.CarrierId equals c.Id
                   join v in db.Vehicles.AsNoTracking() on t.VehicleId equals v.Id
                   join m in db.TripMonitorings.AsNoTracking() on t.Id equals m.TripId into cache
                   from m in cache.DefaultIfEmpty()
                   where r.ContractorId == contractorId && t.Status != TripStatus.Delivered && t.Status != TripStatus.Cancelled
                   select new { Trip = t, Carrier = c, Vehicle = v, Monitoring = m };
        if (query.Search is { Length: > 0 } search)
        {
            var normalized = search.ToUpperInvariant();
            rows = rows.Where(x => ("TRP-" + x.Trip.Id.ToString().Replace("-", "").Substring(0, 6).ToUpper()).Contains(normalized)
                || x.Carrier.CompanyName.ToUpper().Contains(normalized)
                || (x.Carrier.TradeName != null && x.Carrier.TradeName.ToUpper().Contains(normalized))
                || x.Vehicle.Plate.ToUpper().Contains(normalized));
        }
        if (query.Risk is { } risk)
            rows = risk == "NAO_MONITORADO"
                ? rows.Where(x => x.Monitoring == null || x.Monitoring.Risk == null)
                : rows.Where(x => x.Monitoring != null && x.Monitoring.Risk == risk);
        if (query.Status == "ATRASADO") rows = rows.Where(x => x.Monitoring != null && x.Monitoring.Risk == "CRITIC");
        else if (query.Status is { } status)
        {
            var lifecycle = status == "EM_TRANSITO" ? TripStatus.InTransit : TripStatus.AwaitingPickup;
            rows = rows.Where(x => x.Trip.Status == lifecycle && (x.Monitoring == null || x.Monitoring.Risk != "CRITIC"));
        }
        var total = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(x => x.Trip.CreatedAt).ThenBy(x => x.Trip.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new ActiveTripDto(x.Trip.Id, x.Trip.RouteId, "TRP-" + x.Trip.Id.ToString().Replace("-", "").Substring(0, 6).ToUpper(),
                x.Monitoring != null && x.Monitoring.Risk == "CRITIC" ? "ATRASADO" : x.Trip.Status == TripStatus.InTransit ? "EM_TRANSITO" : "AGUARDANDO_COLETA",
                x.Monitoring == null ? "NAO_MONITORADO" : x.Monitoring.Risk ?? "NAO_MONITORADO",
                x.Carrier.TradeName ?? x.Carrier.CompanyName, x.Vehicle.Plate,
                db.TripStops.Where(s => s.TripId == x.Trip.Id).OrderBy(s => s.Sequence).Select(s => s.Address).FirstOrDefault()
                    ?? db.RouteSegments.Where(s => s.RouteId == x.Trip.RouteId).OrderBy(s => s.RouteSequence).ThenBy(s => s.PickupDeadline).ThenBy(s => s.Id).Select(s => s.OriginAddress).FirstOrDefault(),
                db.TripStops.Where(s => s.TripId == x.Trip.Id).OrderByDescending(s => s.Sequence).Select(s => s.Address).FirstOrDefault()
                    ?? db.RouteSegments.Where(s => s.RouteId == x.Trip.RouteId).OrderByDescending(s => s.RouteSequence).ThenByDescending(s => s.PickupDeadline).ThenByDescending(s => s.Id).Select(s => s.DestinationAddress).FirstOrDefault(),
                x.Monitoring == null ? null : x.Monitoring.LastProgressPercentage,
                x.Monitoring == null ? null : x.Monitoring.LastCalculatedEta,
                x.Monitoring == null ? null : x.Monitoring.LastSuccessfulCalculationAt,
                x.Monitoring == null ? null : x.Monitoring.LastPingAt)).ToListAsync(ct);
        return new PagedTripsDto(items, query.Page, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<TripState?> ReadAsync(Guid contractorId, Guid tripId, CancellationToken ct)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            // Split queries must observe one committed snapshot while another request refreshes.
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
            var result = await LoadAsync(db, contractorId, tripId, ct);
            await transaction.CommitAsync(ct);
            return result;
        });
    }

    public async Task<TripDetailDto> RefreshAsync(Guid contractorId, Guid tripId,
        Func<TripState, CancellationToken, Task<TripDetailDto>> refresh, CancellationToken ct)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A fresh context on each retry discards tentative lifecycle/telemetry changes.
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
            var locked = await db.Trips.FromSqlInterpolated($"""
                SELECT t.* FROM "Trip" t
                JOIN "ConsolidatedRoute" r ON r."Id" = t."RouteId"
                WHERE t."Id" = {tripId} AND r."ContractorId" = {contractorId}
                FOR UPDATE OF t
                """).ToListAsync(ct);
            if (locked.Count == 0) throw new TripNotFoundException(tripId);
            var state = await LoadAsync(db, contractorId, tripId, ct) ?? throw new TripNotFoundException(tripId);
            var result = await refresh(state, ct);
            if (state.Snapshot != null && state.Monitoring == null) db.TripMonitorings.Add(state.Snapshot);
            db.TripTelemetries.AddRange(state.NewTelemetry);
            foreach (var item in state.Events.Where(e => db.Entry(e).State == EntityState.Detached)) db.TripMonitoringEvents.Add(item);
            if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        });
    }

    private static async Task<TripState?> LoadAsync(SiglocDbContext db, Guid contractorId, Guid tripId, CancellationToken ct)
    {
        var trip = await db.Trips.Where(t => t.Id == tripId && db.ConsolidatedRoutes.Any(r => r.Id == t.RouteId && r.ContractorId == contractorId))
            .Include(t => t.Stops).ThenInclude(s => s.Actions).AsSplitQuery().SingleOrDefaultAsync(ct);
        if (trip == null) return null;
        var route = await db.ConsolidatedRoutes.SingleAsync(r => r.Id == trip.RouteId, ct);
        var vehicle = await db.Vehicles.SingleAsync(v => v.Id == trip.VehicleId, ct);
        var carrier = await db.Carriers.SingleAsync(c => c.Id == trip.CarrierId, ct);
        var segments = await db.RouteSegments.Where(s => s.RouteId == route.Id).Include(s => s.Items).ThenInclude(i => i.Product).ToListAsync(ct);
        var snapshot = await db.TripMonitorings.SingleOrDefaultAsync(m => m.TripId == trip.Id, ct);
        var events = await db.TripMonitoringEvents.Where(e => e.TripId == trip.Id).OrderByDescending(e => e.OccurredAt).ThenBy(e => e.Id).Take(50).ToListAsync(ct);
        return new TripState(trip, route, vehicle, carrier, segments, snapshot, events);
    }
}
