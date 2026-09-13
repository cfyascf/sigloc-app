using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default);
}