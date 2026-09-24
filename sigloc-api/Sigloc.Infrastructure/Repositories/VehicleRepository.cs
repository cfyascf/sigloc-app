using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly SiglocDbContext _dbContext;

    public VehicleRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, Guid carrierId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.CarrierId == carrierId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Vehicle> Items, int TotalItems)> SearchAsync(
        Guid carrierId,
        string? search,
        OperationalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Vehicles
            .AsNoTracking()
            .Where(v => v.CarrierId == carrierId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(v =>
                v.Plate.ToLower().Contains(term) ||
                v.Model.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(v => v.Plate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<bool> PlateExistsAsync(Guid carrierId, string plate, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vehicles
            .AsNoTracking()
            .AnyAsync(
                v => v.CarrierId == carrierId
                    && v.Plate == plate
                    && (excludeId == null || v.Id != excludeId),
                cancellationToken);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _dbContext.Vehicles.AddAsync(vehicle, cancellationToken);
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

    public async Task<Dictionary<Guid, int>> CountFreeByCarrierIdsAsync(IEnumerable<Guid> carrierIds, CancellationToken cancellationToken = default)
    {
        var ids = carrierIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _dbContext.Vehicles
            .AsNoTracking()
            .Where(v => ids.Contains(v.CarrierId) && v.Status == OperationalStatus.LIVRE)
            .GroupBy(v => v.CarrierId)
            .Select(g => new { CarrierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CarrierId, x => x.Count, cancellationToken);
    }
}
