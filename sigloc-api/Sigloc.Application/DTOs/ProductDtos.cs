using System.Text.Json.Serialization;

namespace Sigloc.Application.DTOs;

/// <summary>
/// Request body for creating/updating a product. Field names follow the API
/// specification (Portuguese); enum-backed fields are received as their wire strings.
/// </summary>
public record ProductRequestDto(
    [property: JsonPropertyName("sku")] string? Sku,
    [property: JsonPropertyName("nome")] string? Name,
    [property: JsonPropertyName("tipo")] string? Type,
    [property: JsonPropertyName("categoria")] string? Category,
    [property: JsonPropertyName("ambienteTransporte")] string? TransportEnvironment,
    [property: JsonPropertyName("tempMin")] double? TempMin,
    [property: JsonPropertyName("tempMax")] double? TempMax,
    [property: JsonPropertyName("tipoEmbalagem")] string? PackagingType,
    [property: JsonPropertyName("perigoso")] bool Dangerous,
    [property: JsonPropertyName("fragil")] bool Fragile,
    [property: JsonPropertyName("pesoPadrao")] double? DefaultWeight,
    [property: JsonPropertyName("volumePadrao")] double? DefaultVolume,
    [property: JsonPropertyName("restricaoManuseio")] string? HandlingRestriction);

/// <summary>Calculated vehicle requirement (never stored, always returned).</summary>
public record VehicleRequirementDto(
    [property: JsonPropertyName("tipoCarroceriaBase")] string BaseBodyworkType,
    [property: JsonPropertyName("nivelRefrigeracaoMinimo")] string MinRefrigerationLevel,
    [property: JsonPropertyName("exigeMopp")] bool RequiresMopp,
    [property: JsonPropertyName("exigeFixacaoCarga")] bool RequiresCargoFixing);

/// <summary>Full product detail returned by POST, GET/{id} and PUT.</summary>
public record ProductResponseDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("contratanteId")] Guid ContractorId,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("tipo")] string? Type,
    [property: JsonPropertyName("categoria")] string Category,
    [property: JsonPropertyName("ambienteTransporte")] string TransportEnvironment,
    [property: JsonPropertyName("tempMin")] double? TempMin,
    [property: JsonPropertyName("tempMax")] double? TempMax,
    [property: JsonPropertyName("tipoEmbalagem")] string? PackagingType,
    [property: JsonPropertyName("perigoso")] bool Dangerous,
    [property: JsonPropertyName("fragil")] bool Fragile,
    [property: JsonPropertyName("pesoPadrao")] double DefaultWeight,
    [property: JsonPropertyName("volumePadrao")] double DefaultVolume,
    [property: JsonPropertyName("restricaoManuseio")] string? HandlingRestriction,
    [property: JsonPropertyName("exigenciaVeiculo")] VehicleRequirementDto VehicleRequirement,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("atualizadoEm")] DateTimeOffset UpdatedAt);

/// <summary>Lightweight product summary used by the list endpoint.</summary>
public record ProductListItemDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("categoria")] string Category,
    [property: JsonPropertyName("ambienteTransporte")] string TransportEnvironment,
    [property: JsonPropertyName("perigoso")] bool Dangerous,
    [property: JsonPropertyName("fragil")] bool Fragile,
    [property: JsonPropertyName("pesoPadrao")] double DefaultWeight,
    [property: JsonPropertyName("volumePadrao")] double DefaultVolume);

/// <summary>Query parameters for the list endpoint.</summary>
public record ProductQueryDto(
    string? Search,
    string? Category,
    int Page = 1,
    int PageSize = 20);

/// <summary>Paged list response.</summary>
public record PagedProductsDto(
    [property: JsonPropertyName("itens")] IReadOnlyList<ProductListItemDto> Items,
    [property: JsonPropertyName("paginaAtual")] int CurrentPage,
    [property: JsonPropertyName("tamanhoPagina")] int PageSize,
    [property: JsonPropertyName("totalItens")] int TotalItems,
    [property: JsonPropertyName("totalPaginas")] int TotalPages);
