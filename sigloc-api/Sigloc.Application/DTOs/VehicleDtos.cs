namespace Sigloc.Application.DTOs;

/// <summary>
/// Request body for creating/updating a vehicle. Enum-backed fields are received as their
/// wire strings; <c>Status</c> is optional and defaults to 'Livre' on create.
/// </summary>
public record VehicleRequestDto(
    string? Plate,
    string? Model,
    int? AxleCount,
    decimal? CapacityWeight,
    decimal? CapacityVolume,
    string? BodyType,
    string? RefrigerationLevel,
    bool HasMopp,
    bool HasCargoSecuring,
    string? Driver,
    string? CurrentLocation,
    string? Status);

/// <summary>Full vehicle detail returned by POST, GET/{id} and PUT.</summary>
public record VehicleResponseDto(
    Guid Id,
    Guid CarrierId,
    string Plate,
    string Model,
    int AxleCount,
    decimal CapacityWeight,
    decimal CapacityVolume,
    string BodyType,
    string RefrigerationLevel,
    bool HasMopp,
    bool HasCargoSecuring,
    string? Driver,
    string? CurrentLocation,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Lightweight vehicle summary used by the list endpoint.</summary>
public record VehicleListItemDto(
    Guid Id,
    string Plate,
    string Model,
    string BodyType,
    string RefrigerationLevel,
    decimal CapacityWeight,
    decimal CapacityVolume,
    string Status);

/// <summary>Query parameters for the list endpoint.</summary>
public record VehicleQueryDto(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20);

/// <summary>Paged list response.</summary>
public record PagedVehiclesDto(
    IReadOnlyList<VehicleListItemDto> Items,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages);
