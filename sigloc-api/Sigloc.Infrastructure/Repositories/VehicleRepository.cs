using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Domain.Enums;

namespace Sigloc.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly SiglocDbContext _dbContext;

    public VehicleRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, Guid transportadoraId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vehicles
            .FirstOrDefaultAsync(p => p.Id == id && p.TransportadoraId == transportadoraId, cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync(Guid transportadoraId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vehicles
            .AsNoTracking()
            .Where(v => v.TransportadoraId == transportadoraId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Vehicle product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Vehicles.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _dbContext.Vehicles.Update(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _dbContext.Vehicles.Remove(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default)
        {
            // Verifica no banco de dados se existe algum veículo com esta placa exata
            return await _dbContext.Vehicles
                .AnyAsync(v => v.Plate == plate, cancellationToken);
        }

    public async Task LockForScheduleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        // Row locks are Postgres-only; SQLite (tests) already serialises writers.
        if (!_dbContext.Database.IsNpgsql())
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Vehicle\" WHERE \"Id\" = {vehicleId} FOR UPDATE",
            cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> CountFreeByCarrierIdsAsync(IEnumerable<Guid> carrierIds, CancellationToken cancellationToken = default)
    {
        var ids = carrierIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        // Free = marked LIVRE and not committed to an active trip.
        return await _dbContext.Vehicles
            .AsNoTracking()
            .Where(v => ids.Contains(v.TransportadoraId)
                && v.Status == OperationalStatus.LIVRE
                && !_dbContext.Trips.Any(t => t.VehicleId == v.Id
                    && (t.Status == TripStatus.AwaitingPickup || t.Status == TripStatus.InTransit)))
            .GroupBy(v => v.TransportadoraId)
            .Select(g => new { CarrierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CarrierId, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetIdsOnActiveTripsAsync(Guid transportadoraId, CancellationToken cancellationToken = default)
    {
        var ids = await _dbContext.Trips
            .AsNoTracking()
            .Where(t => t.CarrierId == transportadoraId
                && (t.Status == TripStatus.AwaitingPickup || t.Status == TripStatus.InTransit))
            .Select(t => t.VehicleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}