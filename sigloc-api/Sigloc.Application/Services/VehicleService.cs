using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<VehicleResponseDto> CreateAsync(CreateVehicleDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Normalizar a placa (maiúsculas, sem espaços ou hifens)
        var normalizedPlate = dto.Plate?.Replace(" ", "").Replace("-", "").ToUpper();

        // 2. Garantir unicidade
        if (await _repository.ExistsByPlateAsync(normalizedPlate, cancellationToken))
        {
            throw new InvalidOperationException($"Já existe um veículo cadastrado com a placa {normalizedPlate}.");
        }

        // Regex que aceita tanto o padrão Mercosul (ex: ABC1D23) quanto o antigo (ex: ABC1234)
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedPlate, @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
        {
            throw new ArgumentException("A placa informada não segue o padrão brasileiro válido.");
        }

        // 3. Criação da Entidade
        var vehicle = new Vehicle(
            dto.TransportadoraId,
            normalizedPlate,
            dto.Model,
            dto.AxleCount,
            dto.CapacityWeight,
            dto.CapacityVolume,
            dto.BodyType,
            dto.RefrigerationLevel,
            dto.HasMopp,
            dto.HasCargoSecuring,
            dto.Driver,
            dto.CurrentLocation
        );

        await _repository.AddAsync(vehicle, cancellationToken);

        return MapToDto(vehicle);
    }

    public async Task<VehicleResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Vehicle with ID {id} not found.");
        }

        return MapToDto(vehicle);
    }

    public async Task<IEnumerable<VehicleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _repository.GetAllAsync(cancellationToken);
        return vehicles.Select(MapToDto);
    }

    public async Task UpdateAsync(Guid id, UpdateVehicleDto dto, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException($"Vehicle with ID {id} not found.");
        }

        vehicle.Update(
            dto.Model, dto.AxleCount, dto.CapacityWeight, dto.CapacityVolume, 
            dto.BodyType, dto.RefrigerationLevel, dto.HasMopp, dto.HasCargoSecuring, 
            dto.Driver, dto.CurrentLocation, dto.Status
        );

        await _repository.UpdateAsync(vehicle, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
{
    var vehicle = await _repository.GetByIdAsync(id, cancellationToken);
    if (vehicle == null)
    {
        throw new KeyNotFoundException($"Vehicle with ID {id} not found.");
    }

    await _repository.DeleteAsync(vehicle, cancellationToken);
}

    // Método para atender o Critério de Aceite: "existir endpoint de validação rápida"
    public async Task<bool> IsPlateAvailableAsync(string plate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plate)) return false;
        
        var normalizedPlate = plate.Replace(" ", "").Replace("-", "").ToUpper();
        var exists = await _repository.ExistsByPlateAsync(normalizedPlate, cancellationToken);
        
        return !exists;
    }

    // Centraliza o mapeamento para evitar repetição de código
    private static VehicleResponseDto MapToDto(Vehicle vehicle)
    {
        return new VehicleResponseDto(
            vehicle.Id,
            vehicle.TransportadoraId,
            vehicle.Plate,
            vehicle.Model,
            vehicle.AxleCount,
            vehicle.CapacityWeight,
            vehicle.CapacityVolume,
            vehicle.BodyType,
            vehicle.RefrigerationLevel,
            vehicle.HasMopp,
            vehicle.HasCargoSecuring,
            vehicle.Driver,
            vehicle.CurrentLocation,
            vehicle.Status
        );
    }
}