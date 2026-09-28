using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IVehicleService    
{
    Task<VehicleResponseDto> CreateAsync(Guid transportadoraId, CreateVehicleDto dto, CancellationToken cancellationToken = default);
    Task<VehicleResponseDto> GetByIdAsync(Guid transportadoraId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleResponseDto>> GetAllAsync(Guid transportadoraId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid transportadoraId, Guid id, UpdateVehicleDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid transportadoraId, Guid id, CancellationToken cancellationToken = default);
}