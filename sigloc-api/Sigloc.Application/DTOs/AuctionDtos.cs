namespace Sigloc.Application.DTOs;

/// <summary>Request body for the route preview and the auction creation endpoints.</summary>
public record RoutePreviewRequestDto(
    IReadOnlyList<Guid>? SegmentIds);

/// <summary>Aggregated physical totals of the consolidated route.</summary>
public record RouteAggregatedTotalsDto(
    double TotalWeightKg,
    double TotalVolumeM3);

/// <summary>
/// Real-time simulation of a consolidated route. Nothing is persisted; distances and
/// durations already stored per segment are summed with the inter-segment legs.
/// </summary>
public record RoutePreviewResponseDto(
    double TotalDistanceKm,
    double EstimatedTimeHours,
    decimal ConsolidatedCeiling,
    decimal EstimatedAnttFloor,
    decimal EstimatedToll,
    decimal CostPerKm,
    RouteAggregatedTotalsDto AggregatedTotals,
    ConsolidatedVehicleRequirementDto ConsolidatedVehicleRequirement);

/// <summary>Request body for creating a consolidated route and starting its auction.</summary>
public record CreateAuctionRequestDto(
    IReadOnlyList<Guid>? SegmentIds,
    DateTimeOffset? ExpiresAt,
    bool AutomaticAward);

/// <summary>Auction snapshot returned after creation.</summary>
public record AuctionDto(
    Guid Id,
    Guid RouteId,
    DateTimeOffset OpenedAt,
    DateTimeOffset ExpiresAt,
    bool AutomaticAward,
    string Status);

/// <summary>Consolidated route snapshot returned after creation.</summary>
public record ConsolidatedRouteDto(
    Guid Id,
    string Status,
    double TotalDistanceKm,
    double EstimatedTimeHours,
    double TotalWeightKg,
    double TotalVolumeM3,
    decimal ConsolidatedCeiling,
    decimal EstimatedAnttFloor);

/// <summary>Response body for a successful auction creation.</summary>
public record CreateAuctionResponseDto(
    AuctionDto Auction,
    ConsolidatedRouteDto ConsolidatedRoute,
    int UpdatedSegments);
