using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, Guid carrierId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Vehicle> Items, int TotalItems)> SearchAsync(
        Guid carrierId,
        string? search,
        OperationalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> PlateExistsAsync(Guid carrierId, string plate, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> CountFreeByCarrierIdsAsync(IEnumerable<Guid> carrierIds, CancellationToken cancellationToken = default);
}
