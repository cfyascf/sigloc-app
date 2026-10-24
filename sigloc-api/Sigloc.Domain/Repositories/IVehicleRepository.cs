using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, Guid transportadoraId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetAllAsync(Guid transportadoraId, CancellationToken cancellationToken = default);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> CountFreeByCarrierIdsAsync(IEnumerable<Guid> carrierIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the vehicle row until the surrounding transaction ends, so a schedule check and the
    /// trip it guards cannot interleave with another award for the same vehicle.
    /// </summary>
    Task LockForScheduleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}