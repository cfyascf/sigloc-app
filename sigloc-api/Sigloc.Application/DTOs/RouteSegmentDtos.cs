namespace Sigloc.Application.DTOs;

/// <summary>Single product line inside a create/update route segment request.</summary>
public record RouteSegmentItemRequestDto(
    Guid? ProductId,
    int? Quantity);

/// <summary>
/// Request body for creating/updating a route segment. Distance, duration and
/// coordinates are computed server-side and are not accepted here.
/// </summary>
public record RouteSegmentRequestDto(
    string? Origin,
    string? Destination,
    decimal? BudgetCeiling,
    decimal? EstimatedTollCost,
    DateTimeOffset? PickupDeadline,
    DateTimeOffset? DeliveryDeadline,
    IReadOnlyList<RouteSegmentItemRequestDto>? Items);

/// <summary>Query parameters for the list endpoint.</summary>
public record RouteSegmentQueryDto(
    string? Origin,
    string? Destination,
    string? Status,
    int Page = 1,
    int PageSize = 20);

/// <summary>Resolved product line returned with computed physical subtotals.</summary>
public record RouteSegmentItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    int Quantity,
    double WeightSubtotal,
    double VolumeSubtotal);

/// <summary>Real-time physical totals of the whole segment.</summary>
public record CalculatedTotalsDto(
    double TotalWeightKg,
    double TotalVolumeM3);

/// <summary>Consolidated vehicle requirement across every product on the segment.</summary>
public record ConsolidatedVehicleRequirementDto(
    string BaseBodyworkType,
    string MinRefrigerationLevel,
    bool RequiresMopp,
    bool RequiresCargoFixing);

/// <summary>Full route segment detail returned by POST, GET/{id}, GET (list) and PUT.</summary>
public record RouteSegmentResponseDto(
    Guid Id,
    Guid ContractorId,
    Guid? RouteId,
    string Origin,
    string Destination,
    double DistanceKm,
    double EstimatedTimeHours,
    decimal? BudgetCeiling,
    decimal EstimatedTollCost,
    DateTimeOffset PickupDeadline,
    DateTimeOffset DeliveryDeadline,
    string Status,
    IReadOnlyList<RouteSegmentItemDto> Items,
    CalculatedTotalsDto CalculatedTotals,
    ConsolidatedVehicleRequirementDto ConsolidatedVehicleRequirement,
    DateTimeOffset CreatedAt);

/// <summary>Paged list response.</summary>
public record PagedRouteSegmentsDto(
    IReadOnlyList<RouteSegmentResponseDto> Items,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages);
