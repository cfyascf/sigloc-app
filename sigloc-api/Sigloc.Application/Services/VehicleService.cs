using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;
    private readonly ICarrierRepository? _carrierRepository;

    // The carrier holds its own RNTRC, which vehicles running under it inherit.
    public VehicleService(IVehicleRepository repository, ICarrierRepository? carrierRepository = null)
    {
        _repository = repository;
        _carrierRepository = carrierRepository;
    }

    private async Task<Carrier?> LoadCarrierAsync(Guid transportadoraId, CancellationToken cancellationToken) =>
        _carrierRepository is null ? null : await _carrierRepository.GetByIdAsync(transportadoraId, cancellationToken);

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
        ValidateEquipment(dto.BodyType, dto.RefrigerationLevel, dto.HasCargoSecuring, "Não foi possível cadastrar o veículo.");
        var carrier = await LoadCarrierAsync(transportadoraId, cancellationToken);
        var documents = ValidateDocuments(dto.Documents ?? new VehicleDocumentsDto(), dto.Driver, dto.HasMopp, carrier,
            creating: true, "Não foi possível cadastrar o veículo.");

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
        ApplyDocuments(vehicle, documents);
        await _repository.AddAsync(vehicle, cancellationToken);

        return MapToDto(vehicle, carrier);
    }

    public async Task<VehicleResponseDto> GetByIdAsync(Guid transportadoraId, Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _repository.GetByIdAsync(id, transportadoraId, cancellationToken);
        if (vehicle == null)
        {
            throw new KeyNotFoundException("Veículo não encontrado.");
        }

        var onActiveTrip = await _repository.GetIdsOnActiveTripsAsync(transportadoraId, cancellationToken);
        var carrier = await LoadCarrierAsync(transportadoraId, cancellationToken);
        return MapToDto(vehicle, carrier, onActiveTrip.Contains(vehicle.Id));
    }

    public async Task<IEnumerable<VehicleResponseDto>> GetAllAsync(Guid transportadoraId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _repository.GetAllAsync(transportadoraId, cancellationToken);
        var onActiveTrip = await _repository.GetIdsOnActiveTripsAsync(transportadoraId, cancellationToken);
        var carrier = await LoadCarrierAsync(transportadoraId, cancellationToken);
        return vehicles.Select(v => MapToDto(v, carrier, onActiveTrip.Contains(v.Id))).ToList();
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
        // Partial updates that don't touch the equipment leave older vehicles (saved before these rules) editable.
        if (dto.BodyType.HasValue || dto.RefrigerationLevel.HasValue || dto.HasCargoSecuring.HasValue)
        {
            ValidateEquipment(
                dto.BodyType ?? vehicle.BodyType,
                dto.RefrigerationLevel ?? vehicle.RefrigerationLevel,
                dto.HasCargoSecuring ?? vehicle.HasCargoSecuring,
                "Não foi possível atualizar o veículo.");
        }
        ValidateMonitoringFields(dto.TraccarDeviceId, dto.DriverPhone);
        if (dto.TraccarDeviceId.HasValue) vehicle.TraccarDeviceId = dto.TraccarDeviceId;
        if (dto.DriverPhone != null) vehicle.DriverPhone = NormalizePhone(dto.DriverPhone);

        // Documents missing on older vehicles stay optional on edit (they only warn); what is sent is checked.
        if (dto.Documents is not null)
        {
            var carrier = await LoadCarrierAsync(transportadoraId, cancellationToken);
            var documents = ValidateDocuments(Merge(vehicle, dto.Documents), dto.Driver ?? vehicle.Driver, dto.HasMopp ?? vehicle.HasMopp,
                carrier, creating: false, "Não foi possível atualizar o veículo.");
            ApplyDocuments(vehicle, documents);
        }

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

    // The body type decides which refrigeration the vehicle can have and whether cargo securing is mandatory.
    private static void ValidateEquipment(VehicleBodyType bodyType, RefrigerationLevel refrigeration, bool hasCargoSecuring, string summary)
    {
        var errors = new List<Sigloc.Application.Exceptions.ValidationError>();
        if (!Enum.IsDefined(bodyType))
        {
            errors.Add(new("bodyType", "Selecione um tipo de carroceria válido."));
        }
        else
        {
            var levels = CargoCompatibility.RefrigerationLevelsFor(bodyType);
            if (!levels.Contains(refrigeration))
                errors.Add(new("refrigerationLevel", $"A carroceria {bodyType.ToLabel()} aceita apenas a refrigeração {string.Join(" ou ", levels)}."));
            if (CargoCompatibility.RequiresCargoSecuring(bodyType) && !hasCargoSecuring)
                errors.Add(new("hasCargoSecuring", $"Obrigatória na carroceria {bodyType.ToLabel()}: a carga deve ser amarrada (CONTRAN Res. 945/2022)."));
        }
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

    private static VehicleDocumentsDto Merge(Vehicle vehicle, VehicleDocumentsDto dto) => new(
        Composition: dto.Composition ?? vehicle.Composition,
        RntrcSource: dto.RntrcSource ?? vehicle.RntrcSource,
        RntrcNumber: dto.RntrcNumber ?? vehicle.RntrcNumber,
        RntrcCategory: dto.RntrcCategory ?? vehicle.RntrcCategory,
        RntrcHolderName: dto.RntrcHolderName ?? vehicle.RntrcHolderName,
        RntrcHolderDocument: dto.RntrcHolderDocument ?? vehicle.RntrcHolderDocument,
        RntrcValidUntil: dto.RntrcValidUntil ?? vehicle.RntrcValidUntil,
        DriverCnhCategory: dto.DriverCnhCategory ?? vehicle.DriverCnhCategory,
        DriverHasEar: dto.DriverHasEar ?? vehicle.DriverHasEar,
        DriverToxicologicalExamValidUntil: dto.DriverToxicologicalExamValidUntil ?? vehicle.DriverToxicologicalExamValidUntil,
        MoppValidUntil: dto.MoppValidUntil ?? vehicle.MoppValidUntil,
        CivCippValidUntil: dto.CivCippValidUntil ?? vehicle.CivCippValidUntil);

    internal const int MaxHolderNameLength = 120;

    /// <summary>
    /// Checks the documents and returns them normalised (RNTRC with 8 digits, document digits only,
    /// no third-party data when the vehicle runs under the carrier's RNTRC). A new vehicle must
    /// have them all (the driver's CNH only when a driver is assigned); on edit, what is missing
    /// stays missing and only what is filled in is checked.
    /// </summary>
    private static VehicleDocumentsDto ValidateDocuments(
        VehicleDocumentsDto dto, string? driver, bool hasMopp, Carrier? carrier, bool creating, string summary)
    {
        var errors = new List<Sigloc.Application.Exceptions.ValidationError>();

        if (dto.Composition is VehicleComposition composition && !Enum.IsDefined(composition))
            errors.Add(new("composition", "Selecione uma composição válida."));
        else if (creating && dto.Composition is null)
            errors.Add(new("composition", "Informe a composição do veículo."));

        string? rntrcNumber = null;
        string? holderDocument = null;
        if (dto.RntrcSource is RntrcSource source && !Enum.IsDefined(source))
        {
            errors.Add(new("rntrcSource", "Selecione de quem é o RNTRC do veículo."));
        }
        else if (dto.RntrcSource == RntrcSource.Carrier)
        {
            if (creating && string.IsNullOrEmpty(carrier?.RntrcNumber))
                errors.Add(new("rntrcSource", "A transportadora ainda não cadastrou o próprio RNTRC no Perfil. Cadastre-o lá ou informe o RNTRC de terceiro."));
        }
        else if (dto.RntrcSource == RntrcSource.ThirdParty)
        {
            rntrcNumber = TransportDocuments.NormalizeRntrc(dto.RntrcNumber);
            if (rntrcNumber is null)
                errors.Add(new("rntrcNumber", "Informe o RNTRC com 8 dígitos."));
            if (dto.RntrcCategory is not RntrcCategory category || !Enum.IsDefined(category))
                errors.Add(new("rntrcCategory", "Informe a categoria do RNTRC (TAC, ETC ou CTC)."));
            if (string.IsNullOrWhiteSpace(dto.RntrcHolderName))
                errors.Add(new("rntrcHolderName", "Informe o titular do RNTRC."));
            else if (dto.RntrcHolderName.Trim().Length > MaxHolderNameLength)
                errors.Add(new("rntrcHolderName", $"O titular deve ter no máximo {MaxHolderNameLength} caracteres."));

            holderDocument = new string((dto.RntrcHolderDocument ?? "").Where(char.IsAsciiDigit).ToArray());
            // A TAC is a person (CPF); companies and cooperatives have a CNPJ.
            var individual = dto.RntrcCategory == RntrcCategory.TAC;
            if (individual ? !TransportDocuments.IsValidCpf(holderDocument) : !CnpjValidator.IsValid(holderDocument))
                errors.Add(new("rntrcHolderDocument", individual
                    ? "Informe um CPF válido: o RNTRC de TAC é de pessoa física."
                    : "Informe um CNPJ válido: o RNTRC de ETC e CTC é de pessoa jurídica."));
            if (dto.RntrcValidUntil is null)
                errors.Add(new("rntrcValidUntil", "Informe a data de revalidação do RNTRC."));
        }
        else if (creating)
        {
            errors.Add(new("rntrcSource", "Informe de quem é o RNTRC do veículo."));
        }

        if (dto.DriverCnhCategory is CnhCategory cnh && !Enum.IsDefined(cnh))
        {
            errors.Add(new("driverCnhCategory", "Selecione uma categoria de CNH válida."));
        }
        else if (dto.DriverCnhCategory is CnhCategory known && dto.Composition is VehicleComposition combination
            && combination != VehicleComposition.Rigid && known != CnhCategory.E)
        {
            errors.Add(new("driverCnhCategory", $"{combination.ToLabel()} exige motorista com CNH categoria E."));
        }

        if (creating && !string.IsNullOrWhiteSpace(driver))
        {
            if (dto.DriverCnhCategory is null) errors.Add(new("driverCnhCategory", "Informe a categoria da CNH do motorista."));
            if (dto.DriverHasEar is null) errors.Add(new("driverHasEar", "Informe se a CNH do motorista tem EAR."));
            if (dto.DriverToxicologicalExamValidUntil is null)
                errors.Add(new("driverToxicologicalExamValidUntil", "Informe a validade do exame toxicológico do motorista."));
        }

        if (creating && hasMopp && dto.MoppValidUntil is null)
            errors.Add(new("moppValidUntil", "Informe a validade do MOPP do motorista."));

        if (errors.Count > 0) throw new Sigloc.Application.Exceptions.ValidationException(errors, summary);

        var thirdParty = dto.RntrcSource == RntrcSource.ThirdParty;
        return dto with
        {
            RntrcNumber = thirdParty ? rntrcNumber : null,
            RntrcCategory = thirdParty ? dto.RntrcCategory : null,
            RntrcHolderName = thirdParty ? dto.RntrcHolderName!.Trim() : null,
            RntrcHolderDocument = thirdParty ? holderDocument : null,
            RntrcValidUntil = thirdParty ? dto.RntrcValidUntil : null,
        };
    }

    private static void ApplyDocuments(Vehicle vehicle, VehicleDocumentsDto documents)
    {
        vehicle.Composition = documents.Composition;
        vehicle.RntrcSource = documents.RntrcSource;
        vehicle.RntrcNumber = documents.RntrcNumber;
        vehicle.RntrcCategory = documents.RntrcCategory;
        vehicle.RntrcHolderName = documents.RntrcHolderName;
        vehicle.RntrcHolderDocument = documents.RntrcHolderDocument;
        vehicle.RntrcValidUntil = documents.RntrcValidUntil;
        vehicle.DriverCnhCategory = documents.DriverCnhCategory;
        vehicle.DriverHasEar = documents.DriverHasEar;
        vehicle.DriverToxicologicalExamValidUntil = documents.DriverToxicologicalExamValidUntil;
        vehicle.MoppValidUntil = documents.MoppValidUntil;
        vehicle.CivCippValidUntil = documents.CivCippValidUntil;
    }

    private static VehicleDocumentsDto DocumentsOf(Vehicle vehicle) => new(
        vehicle.Composition, vehicle.RntrcSource, vehicle.RntrcNumber, vehicle.RntrcCategory, vehicle.RntrcHolderName,
        vehicle.RntrcHolderDocument, vehicle.RntrcValidUntil, vehicle.DriverCnhCategory, vehicle.DriverHasEar,
        vehicle.DriverToxicologicalExamValidUntil, vehicle.MoppValidUntil, vehicle.CivCippValidUntil);

    internal static IReadOnlyList<ComplianceIssueDto> ToDto(IEnumerable<ComplianceIssue> issues) =>
        issues.Select(i => new ComplianceIssueDto(i.Code, i.Severity == ComplianceSeverity.Blocking ? "BLOCKING" : "WARNING", i.Message))
            .ToList();

    private static VehicleResponseDto MapToDto(Vehicle vehicle, Carrier? carrier, bool onActiveTrip = false)
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
            onActiveTrip,
            DocumentsOf(vehicle),
            ToDto(VehicleCompliance.Check(vehicle, carrier, BrasiliaTime.Today))
        );
    }
}