using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
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

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vehicles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vehicles
            .AsNoTracking() 
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Vehicle product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Vehicles.AddAsync(product, cancellationToken);
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
}