using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IVehicleService    
{
    Task<VehicleResponseDto> CreateAsync(CreateVehicleDto dto, CancellationToken cancellationToken = default);
    Task<VehicleResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateVehicleDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}