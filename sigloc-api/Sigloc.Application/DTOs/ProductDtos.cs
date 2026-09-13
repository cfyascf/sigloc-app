namespace Sigloc.Application.DTOs;

/// <summary>
/// Request body for creating/updating a product. Field names follow the API
/// specification (Portuguese); enum-backed fields are received as their wire strings.
/// </summary>
public record ProductRequestDto(
    string? Sku,
    string? Name,
    string? Type,
    string? Category,
    string? TransportEnvironment,
    double? TempMin,
    double? TempMax,
    string? PackagingType,
    bool Dangerous,
    bool Fragile,
    double? DefaultWeight,
    double? DefaultVolume,
    string? HandlingRestriction);

/// <summary>Calculated vehicle requirement (never stored, always returned).</summary>
public record VehicleRequirementDto(
    string BaseBodyworkType,
    string MinRefrigerationLevel,
    bool RequiresMopp,
    bool RequiresCargoFixing);

/// <summary>Full product detail returned by POST, GET/{id} and PUT.</summary>
public record ProductResponseDto(
    Guid Id,
    Guid ContractorId,
    string Sku,
    string Name,
    string? Type,
    string Category,
    string TransportEnvironment,
    double? TempMin,
    double? TempMax,
    string? PackagingType,
    bool Dangerous,
    bool Fragile,
    double DefaultWeight,
    double DefaultVolume,
    string? HandlingRestriction,
    VehicleRequirementDto VehicleRequirement,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Lightweight product summary used by the list endpoint.</summary>
public record ProductListItemDto(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    string TransportEnvironment,
    bool Dangerous,
    bool Fragile,
    double DefaultWeight,
    double DefaultVolume);

/// <summary>Query parameters for the list endpoint.</summary>
public record ProductQueryDto(
    string? Search,
    string? Category,
    int Page = 1,
    int PageSize = 20);

/// <summary>Paged list response.</summary>
public record PagedProductsDto(
    IReadOnlyList<ProductListItemDto> Items,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages);
