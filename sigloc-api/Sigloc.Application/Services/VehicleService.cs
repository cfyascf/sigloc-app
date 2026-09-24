using System.Text.RegularExpressions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class VehicleService : IVehicleService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    // Accepts both the Mercosul (ABC1D23) and the legacy (ABC1234) Brazilian plate formats.
    private static readonly Regex PlatePattern = new("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.Compiled);

    private const string CreateErrorMessage = "Não foi possível cadastrar o veículo.";

    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<VehicleResponseDto> CreateAsync(Guid carrierId, VehicleRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validated = Validate(dto);

        if (await _repository.PlateExistsAsync(carrierId, validated.Plate, excludeId: null, cancellationToken))
        {
            throw new DuplicatePlateException(validated.Plate);
        }

        var vehicle = new Vehicle
        {
            CarrierId = carrierId,
            Plate = validated.Plate,
            Model = validated.Model,
            AxleCount = validated.AxleCount,
            CapacityWeight = validated.CapacityWeight,
            CapacityVolume = validated.CapacityVolume,
            BodyType = validated.BodyType,
            RefrigerationLevel = validated.RefrigerationLevel,
            HasMopp = dto.HasMopp,
            HasCargoSecuring = dto.HasCargoSecuring,
            Driver = validated.Driver,
            CurrentLocation = validated.CurrentLocation,
            Status = validated.Status
        };

        await _repository.AddAsync(vehicle, cancellationToken);

        return MapToResponse(vehicle);
    }

    public async Task<VehicleResponseDto> GetByIdAsync(Guid carrierId, Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, carrierId, cancellationToken)
            ?? throw new VehicleNotFoundException(id);

        return MapToResponse(vehicle);
    }

    public async Task<PagedVehiclesDto> SearchAsync(Guid carrierId, VehicleQueryDto query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        OperationalStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!VehicleEnumMappings.TryParseStatus(query.Status, out var parsed))
            {
                throw new ValidationException(new[]
                {
                    new ValidationError("status", "Valor inválido. Use 'Livre', 'EmTransito' ou 'Manutencao'.")
                });
            }

            status = parsed;
        }

        var (items, totalItems) = await _repository.SearchAsync(
            carrierId, query.Search, status, page, pageSize, cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var mapped = items.Select(MapToListItem).ToList();

        return new PagedVehiclesDto(mapped, page, pageSize, totalItems, totalPages);
    }

    public async Task<VehicleResponseDto> UpdateAsync(Guid carrierId, Guid id, VehicleRequestDto dto, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, carrierId, cancellationToken)
            ?? throw new VehicleNotFoundException(id);

        var validated = Validate(dto);

        if (await _repository.PlateExistsAsync(carrierId, validated.Plate, excludeId: id, cancellationToken))
        {
            throw new DuplicatePlateException(validated.Plate);
        }

        vehicle.Plate = validated.Plate;
        vehicle.Model = validated.Model;
        vehicle.AxleCount = validated.AxleCount;
        vehicle.CapacityWeight = validated.CapacityWeight;
        vehicle.CapacityVolume = validated.CapacityVolume;
        vehicle.BodyType = validated.BodyType;
        vehicle.RefrigerationLevel = validated.RefrigerationLevel;
        vehicle.HasMopp = dto.HasMopp;
        vehicle.HasCargoSecuring = dto.HasCargoSecuring;
        vehicle.Driver = validated.Driver;
        vehicle.CurrentLocation = validated.CurrentLocation;
        vehicle.Status = validated.Status;

        await _repository.UpdateAsync(vehicle, cancellationToken);

        return MapToResponse(vehicle);
    }

    public async Task DeleteAsync(Guid carrierId, Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, carrierId, cancellationToken)
            ?? throw new VehicleNotFoundException(id);

        await _repository.DeleteAsync(vehicle, cancellationToken);
    }

    public async Task<bool> IsPlateAvailableAsync(Guid carrierId, string? plate, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePlate(plate);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return !await _repository.PlateExistsAsync(carrierId, normalized, excludeId: null, cancellationToken);
    }

    private static ValidatedVehicle Validate(VehicleRequestDto dto)
    {
        var errors = new List<ValidationError>();

        var plate = NormalizePlate(dto.Plate);
        if (string.IsNullOrWhiteSpace(plate))
        {
            errors.Add(new ValidationError("placa", "Obrigatório."));
        }
        else if (!PlatePattern.IsMatch(plate))
        {
            errors.Add(new ValidationError("placa", "A placa informada não segue o padrão brasileiro válido."));
        }

        var model = dto.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            errors.Add(new ValidationError("modelo", "Obrigatório."));
        }

        var bodyTypeParsed = VehicleEnumMappings.TryParseBodyType(dto.BodyType, out var bodyType);
        if (!bodyTypeParsed)
        {
            errors.Add(new ValidationError("tipoCarroceria", "Obrigatório. Valor inválido."));
        }

        var refrigerationParsed = VehicleEnumMappings.TryParseRefrigeration(dto.RefrigerationLevel, out var refrigeration);
        if (!refrigerationParsed)
        {
            errors.Add(new ValidationError("nivelRefrigeracao", "Obrigatório. Use 'Nenhuma', 'Resfriado' ou 'Congelado'."));
        }

        // Status is optional; defaults to Livre when omitted.
        var status = OperationalStatus.LIVRE;
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            if (!VehicleEnumMappings.TryParseStatus(dto.Status, out status))
            {
                errors.Add(new ValidationError("status", "Valor inválido. Use 'Livre', 'EmTransito' ou 'Manutencao'."));
            }
        }

        if (dto.AxleCount is not int axleCount || axleCount <= 0)
        {
            errors.Add(new ValidationError("quantidadeEixos", "Deve ser maior que zero."));
        }

        if (dto.CapacityWeight is not decimal weight || weight <= 0)
        {
            errors.Add(new ValidationError("capacidadePeso", "Deve ser maior que zero."));
        }

        if (dto.CapacityVolume is not decimal volume || volume <= 0)
        {
            errors.Add(new ValidationError("capacidadeVolume", "Deve ser maior que zero."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors, CreateErrorMessage);
        }

        return new ValidatedVehicle(
            plate!,
            model!,
            dto.AxleCount!.Value,
            dto.CapacityWeight!.Value,
            dto.CapacityVolume!.Value,
            bodyType,
            refrigeration,
            string.IsNullOrWhiteSpace(dto.Driver) ? null : dto.Driver!.Trim(),
            string.IsNullOrWhiteSpace(dto.CurrentLocation) ? null : dto.CurrentLocation!.Trim(),
            status);
    }

    private static string? NormalizePlate(string? plate)
        => plate?.Replace(" ", "").Replace("-", "").ToUpperInvariant();

    private static VehicleResponseDto MapToResponse(Vehicle vehicle)
    {
        return new VehicleResponseDto(
            vehicle.Id,
            vehicle.CarrierId,
            vehicle.Plate,
            vehicle.Model,
            vehicle.AxleCount,
            vehicle.CapacityWeight,
            vehicle.CapacityVolume,
            vehicle.BodyType.ToWire(),
            vehicle.RefrigerationLevel.ToWire(),
            vehicle.HasMopp,
            vehicle.HasCargoSecuring,
            vehicle.Driver,
            vehicle.CurrentLocation,
            vehicle.Status.ToWire(),
            vehicle.CreatedAt,
            vehicle.UpdatedAt);
    }

    private static VehicleListItemDto MapToListItem(Vehicle vehicle)
    {
        return new VehicleListItemDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Model,
            vehicle.BodyType.ToWire(),
            vehicle.RefrigerationLevel.ToWire(),
            vehicle.CapacityWeight,
            vehicle.CapacityVolume,
            vehicle.Status.ToWire());
    }

    private sealed record ValidatedVehicle(
        string Plate,
        string Model,
        int AxleCount,
        decimal CapacityWeight,
        decimal CapacityVolume,
        VehicleBodyType BodyType,
        RefrigerationLevel RefrigerationLevel,
        string? Driver,
        string? CurrentLocation,
        OperationalStatus Status);
}
