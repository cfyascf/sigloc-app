using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IVehicleService
{
    Task<VehicleResponseDto> CreateAsync(Guid carrierId, VehicleRequestDto dto, CancellationToken cancellationToken = default);
    Task<VehicleResponseDto> GetByIdAsync(Guid carrierId, Guid id, CancellationToken cancellationToken = default);
    Task<PagedVehiclesDto> SearchAsync(Guid carrierId, VehicleQueryDto query, CancellationToken cancellationToken = default);
    Task<VehicleResponseDto> UpdateAsync(Guid carrierId, Guid id, VehicleRequestDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid carrierId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsPlateAvailableAsync(Guid carrierId, string? plate, CancellationToken cancellationToken = default);
}
