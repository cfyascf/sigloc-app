using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class ProductService : IProductService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductResponseDto> CreateAsync(Guid contractorId, ProductRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validated = Validate(dto);

        if (await _repository.SkuExistsAsync(contractorId, validated.Sku, excludeId: null, cancellationToken))
        {
            throw new DuplicateSkuException(validated.Sku);
        }

        var product = new Product
        {
            ContractorId = contractorId,
            Sku = validated.Sku,
            Name = validated.Name,
            Type = validated.Type,
            Category = validated.Category,
            TransportEnvironment = validated.TransportEnvironment,
            TempMin = validated.TempMin,
            TempMax = validated.TempMax,
            PackagingType = validated.PackagingType,
            Dangerous = dto.Dangerous,
            Fragile = dto.Fragile,
            DefaultWeight = validated.DefaultWeight,
            DefaultVolume = validated.DefaultVolume,
            HandlingRestriction = string.IsNullOrWhiteSpace(dto.HandlingRestriction) ? null : dto.HandlingRestriction
        };

        await _repository.AddAsync(product, cancellationToken);

        return MapToResponse(product);
    }

    public async Task<ProductResponseDto> GetByIdAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, contractorId, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        return MapToResponse(product);
    }

    public async Task<PagedProductsDto> SearchAsync(Guid contractorId, ProductQueryDto query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        ProductCategory? category = null;
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!ProductEnumMappings.TryParseCategory(query.Category, out var parsed))
            {
                throw new ValidationException(new[]
                {
                    new ValidationError("categoria", "Valor inválido. Use 'Geral', 'GranelSolido' ou 'GranelLiquido'.")
                });
            }

            category = parsed;
        }

        var (items, totalItems) = await _repository.SearchAsync(
            contractorId, query.Search, category, page, pageSize, cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var mapped = items.Select(MapToListItem).ToList();

        return new PagedProductsDto(mapped, page, pageSize, totalItems, totalPages);
    }

    public async Task<ProductResponseDto> UpdateAsync(Guid contractorId, Guid id, ProductRequestDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, contractorId, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        var validated = Validate(dto);

        if (await _repository.SkuExistsAsync(contractorId, validated.Sku, excludeId: id, cancellationToken))
        {
            throw new DuplicateSkuException(validated.Sku);
        }

        product.Sku = validated.Sku;
        product.Name = validated.Name;
        product.Type = validated.Type;
        product.Category = validated.Category;
        product.TransportEnvironment = validated.TransportEnvironment;
        product.TempMin = validated.TempMin;
        product.TempMax = validated.TempMax;
        product.PackagingType = validated.PackagingType;
        product.Dangerous = dto.Dangerous;
        product.Fragile = dto.Fragile;
        product.DefaultWeight = validated.DefaultWeight;
        product.DefaultVolume = validated.DefaultVolume;
        product.HandlingRestriction = string.IsNullOrWhiteSpace(dto.HandlingRestriction) ? null : dto.HandlingRestriction;

        await _repository.UpdateAsync(product, cancellationToken);

        return MapToResponse(product);
    }

    public async Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, contractorId, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        var linkedSegments = await _repository.GetLinkedSegmentIdsAsync(id, cancellationToken);
        if (linkedSegments.Count > 0)
        {
            throw new ProductInUseException(linkedSegments);
        }

        await _repository.DeleteAsync(product, cancellationToken);
    }

    private static ValidatedProduct Validate(ProductRequestDto dto)
    {
        var errors = new List<ValidationError>();

        var sku = dto.Sku?.Trim();
        if (string.IsNullOrWhiteSpace(sku))
        {
            errors.Add(new ValidationError("sku", "Obrigatório."));
        }

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new ValidationError("nome", "Obrigatório."));
        }

        var categoryParsed = ProductEnumMappings.TryParseCategory(dto.Category, out var category);
        if (!categoryParsed)
        {
            errors.Add(new ValidationError("categoria", "Obrigatório. Use 'Geral', 'GranelSolido' ou 'GranelLiquido'."));
        }

        var environmentParsed = ProductEnumMappings.TryParseEnvironment(dto.TransportEnvironment, out var environment);
        if (!environmentParsed)
        {
            errors.Add(new ValidationError("ambienteTransporte", "Obrigatório. Use 'Seco', 'Resfriado' ou 'Congelado'."));
        }

        // Temperature range: required when environment is not Dry; zeroed/ignored when Dry.
        double? tempMin = null;
        double? tempMax = null;
        if (environmentParsed && environment != TransportEnvironment.Dry)
        {
            if (dto.TempMin is null)
            {
                errors.Add(new ValidationError("tempMin", "Obrigatório quando ambienteTransporte é 'Resfriado' ou 'Congelado'."));
            }

            if (dto.TempMax is null)
            {
                errors.Add(new ValidationError("tempMax", "Obrigatório quando ambienteTransporte é 'Resfriado' ou 'Congelado'."));
            }

            if (dto.TempMin is double min && dto.TempMax is double max && min > max)
            {
                errors.Add(new ValidationError("tempMin", "Deve ser menor ou igual a tempMax."));
            }

            tempMin = dto.TempMin;
            tempMax = dto.TempMax;
        }

        // Packaging type: required when category is General; ignored otherwise.
        PackagingType? packagingType = null;
        if (categoryParsed && category == ProductCategory.General)
        {
            if (!ProductEnumMappings.TryParsePackaging(dto.PackagingType, out var parsedPackaging))
            {
                errors.Add(new ValidationError("tipoEmbalagem", "Obrigatório quando categoria é 'Geral'."));
            }
            else
            {
                packagingType = parsedPackaging;
            }
        }

        if (dto.DefaultWeight is not double weight || weight <= 0)
        {
            errors.Add(new ValidationError("pesoPadrao", "Deve ser maior que zero."));
        }

        if (dto.DefaultVolume is not double volume || volume <= 0)
        {
            errors.Add(new ValidationError("volumePadrao", "Deve ser maior que zero."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new ValidatedProduct(
            sku!,
            name!,
            string.IsNullOrWhiteSpace(dto.Type) ? null : dto.Type!.Trim(),
            category,
            environment,
            tempMin,
            tempMax,
            packagingType,
            dto.DefaultWeight!.Value,
            dto.DefaultVolume!.Value);
    }

    private static ProductResponseDto MapToResponse(Product product)
    {
        var requirement = VehicleRequirement.From(product);

        return new ProductResponseDto(
            product.Id,
            product.ContractorId,
            product.Sku,
            product.Name,
            product.Type,
            product.Category.ToWire(),
            product.TransportEnvironment.ToWire(),
            product.TempMin,
            product.TempMax,
            product.PackagingType?.ToWire(),
            product.Dangerous,
            product.Fragile,
            product.DefaultWeight,
            product.DefaultVolume,
            product.HandlingRestriction,
            new VehicleRequirementDto(
                requirement.BaseBodyworkType,
                requirement.MinRefrigerationLevel,
                requirement.RequiresMopp,
                requirement.RequiresCargoFixing),
            product.CreatedAt,
            product.UpdatedAt);
    }

    private static ProductListItemDto MapToListItem(Product product)
    {
        return new ProductListItemDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Category.ToWire(),
            product.TransportEnvironment.ToWire(),
            product.Dangerous,
            product.Fragile,
            product.DefaultWeight,
            product.DefaultVolume);
    }

    private sealed record ValidatedProduct(
        string Sku,
        string Name,
        string? Type,
        ProductCategory Category,
        TransportEnvironment TransportEnvironment,
        double? TempMin,
        double? TempMax,
        PackagingType? PackagingType,
        double DefaultWeight,
        double DefaultVolume);
}
