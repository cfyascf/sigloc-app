using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<VehicleResponseDto> CreateAsync(Guid transportadoraId, CreateVehicleDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Normalizar a placa (maiúsculas, sem espaços ou hifens)
        var normalizedPlate = dto.Plate?.Replace(" ", "").Replace("-", "").ToUpper();

        // 2. Garantir unicidade
        if (await _repository.ExistsByPlateAsync(normalizedPlate, cancellationToken))
        {
            throw new Sigloc.Application.Exceptions.DuplicatePlateException(normalizedPlate!);
        }

        // Regex que aceita tanto o padrão Mercosul (ex: ABC1D23) quanto o antigo (ex: ABC1234)
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedPlate, @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
        {
            throw new ArgumentException("A placa informada não segue o padrão brasileiro válido.");
        }

        ValidateFields(dto.Model, dto.AxleCount, dto.CapacityWeight, dto.CapacityVolume, dto.Driver, dto.CurrentLocation,
            requireModel: true, "Não foi possível cadastrar o veículo.");

        // 3. Criação da Entidade
        var vehicle = new Vehicle(
            transportadoraId,
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

        ValidateMonitoringFields(dto.TraccarDeviceId, dto.DriverPhone);
        vehicle.TraccarDeviceId = dto.TraccarDeviceId;
        vehicle.DriverPhone = NormalizePhone(dto.DriverPhone);
        await _repository.AddAsync(vehicle, cancellationToken);

        return MapToDto(vehicle);
    }

    public async Task<VehicleResponseDto> GetByIdAsync(Guid transportadoraId, Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, transportadoraId, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException("Veículo não encontrado.");
        }

        var onActiveTrip = await _repository.GetIdsOnActiveTripsAsync(transportadoraId, cancellationToken);
        return MapToDto(vehicle, onActiveTrip.Contains(vehicle.Id));
    }

    public async Task<IEnumerable<VehicleResponseDto>> GetAllAsync(Guid transportadoraId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _repository.GetAllAsync(transportadoraId, cancellationToken);
        var onActiveTrip = await _repository.GetIdsOnActiveTripsAsync(transportadoraId, cancellationToken);
        return vehicles.Select(v => MapToDto(v, onActiveTrip.Contains(v.Id)));
    }

    public async Task UpdateAsync(Guid transportadoraId, Guid id, UpdateVehicleDto dto, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, transportadoraId, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException("Veículo não encontrado.");
        }

        if (dto.Status == OperationalStatus.EM_TRANSITO)
        {
            throw new Sigloc.Application.Exceptions.ValidationException(
                [new("status", "\"Em viagem\" é definido automaticamente pelas viagens. Use Livre ou Manutenção.")],
                "Não foi possível atualizar o veículo.");
        }

        ValidateFields(dto.Model, dto.AxleCount, dto.CapacityWeight, dto.CapacityVolume, dto.Driver, dto.CurrentLocation,
            requireModel: false, "Não foi possível atualizar o veículo.");
        ValidateMonitoringFields(dto.TraccarDeviceId, dto.DriverPhone);
        if (dto.TraccarDeviceId.HasValue) vehicle.TraccarDeviceId = dto.TraccarDeviceId;
        if (dto.DriverPhone != null) vehicle.DriverPhone = NormalizePhone(dto.DriverPhone);

        vehicle.Update(
            dto.Model, dto.AxleCount, dto.CapacityWeight, dto.CapacityVolume, 
            dto.BodyType, dto.RefrigerationLevel, dto.HasMopp, dto.HasCargoSecuring, 
            dto.Driver, dto.CurrentLocation, dto.Status
        );

        await _repository.UpdateAsync(vehicle, cancellationToken);
    }

    public async Task DeleteAsync(Guid transportadoraId, Guid id, CancellationToken cancellationToken = default)
{
    var vehicle = await _repository.GetByIdAsync(id, transportadoraId, cancellationToken);
    if (vehicle == null)
    {
        throw new KeyNotFoundException("Veículo não encontrado.");
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
    private static string? NormalizePhone(string? phone) => string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

    // Upper bounds keep typos (e.g. 999.999.999 kg) out of matching and occupancy figures. They are
    // generous on purpose: a rodotrem carries ~74 t on 9 axles.
    internal const int MaxAxleCount = 12;
    internal const decimal MaxCapacityWeightKg = 100_000m;
    internal const decimal MaxCapacityVolumeM3 = 500m;
    internal const int MaxModelLength = 100;
    internal const int MaxDriverLength = 100;
    internal const int MaxLocationLength = 200;

    // Reported per field (not as one exception from the Vehicle constructor) so the forms can show
    // each message under its input. Null values are "not sent" on update and are skipped.
    private static void ValidateFields(
        string? model, int? axleCount, decimal? capacityWeight, decimal? capacityVolume,
        string? driver, string? currentLocation, bool requireModel, string summary)
    {
        var errors = new List<Sigloc.Application.Exceptions.ValidationError>();
        if (requireModel && string.IsNullOrWhiteSpace(model)) errors.Add(new("model", "Informe a marca/modelo."));
        if (model?.Trim().Length > MaxModelLength) errors.Add(new("model", $"A marca/modelo deve ter no máximo {MaxModelLength} caracteres."));
        if (axleCount is <= 0) errors.Add(new("axleCount", "A quantidade de eixos deve ser maior que zero."));
        if (axleCount > MaxAxleCount) errors.Add(new("axleCount", $"A quantidade de eixos deve ser no máximo {MaxAxleCount}."));
        if (capacityWeight is <= 0) errors.Add(new("capacityWeight", "A capacidade de peso deve ser maior que zero."));
        if (capacityWeight > MaxCapacityWeightKg) errors.Add(new("capacityWeight", $"A capacidade de peso deve ser no máximo 100.000 kg."));
        if (capacityVolume is <= 0) errors.Add(new("capacityVolume", "A capacidade de volume deve ser maior que zero."));
        if (capacityVolume > MaxCapacityVolumeM3) errors.Add(new("capacityVolume", $"A capacidade de volume deve ser no máximo 500 m³."));
        if (driver?.Trim().Length > MaxDriverLength) errors.Add(new("driver", $"O nome do motorista deve ter no máximo {MaxDriverLength} caracteres."));
        if (currentLocation?.Trim().Length > MaxLocationLength) errors.Add(new("currentLocation", $"A localização deve ter no máximo {MaxLocationLength} caracteres."));
        if (errors.Count > 0) throw new Sigloc.Application.Exceptions.ValidationException(errors, summary);
    }

    private static void ValidateMonitoringFields(long? deviceId, string? phone)
    {
        var errors = new List<Sigloc.Application.Exceptions.ValidationError>();
        if (deviceId is <= 0) errors.Add(new("traccarDeviceId", "O ID do dispositivo Traccar deve ser um número positivo."));
        if (phone != null && (phone.Length > 40 || phone.Any(c => !char.IsDigit(c) && !" +-().".Contains(c))))
            errors.Add(new("driverPhone", "Telefone inválido: use apenas números, espaços e + - ( ) ., com até 40 caracteres."));
        if (errors.Count > 0) throw new Sigloc.Application.Exceptions.ValidationException(errors, "Dados de rastreamento do veículo inválidos.");
    }

    private static VehicleResponseDto MapToDto(Vehicle vehicle, bool onActiveTrip = false)
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
            vehicle.Status,
            vehicle.TraccarDeviceId,
            vehicle.DriverPhone,
            onActiveTrip
        );
    }
}