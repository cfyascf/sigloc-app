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

// ---------------------------------------------------------------------------
// Auction listing (Painel Principal) — GET /api/auctions
// ---------------------------------------------------------------------------

/// <summary>Query parameters for the auction listing endpoint.</summary>
public record AuctionQueryDto(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20);

/// <summary>Aggregated bid figures shown in the listing.</summary>
public record BidMetricsDto(
    decimal? BestBid,
    int TotalBids);

/// <summary>A single auction row in the management listing.</summary>
public record AuctionListItemDto(
    Guid Id,
    Guid RouteId,
    string Status,
    string ItinerarySummary,
    string ItineraryWithStates,
    IReadOnlyList<Guid> LinkedSegments,
    string RiskIndicator,
    DateTimeOffset ExpiresAt,
    BidMetricsDto BidMetrics);

/// <summary>Paged auction listing response.</summary>
public record PagedAuctionsDto(
    IReadOnlyList<AuctionListItemDto> Items,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages);

// ---------------------------------------------------------------------------
// Auction detail (Detalhes da Rota) — GET /api/auctions/{id}
// ---------------------------------------------------------------------------

/// <summary>Financial intelligence snapshot read from the consolidated route.</summary>
public record FinancialScenarioDto(
    decimal ConsolidatedCeiling,
    decimal EstimatedAnttFloor);

/// <summary>Consolidated route block of the auction detail.</summary>
public record AuctionDetailRouteDto(
    Guid Id,
    string Status,
    string FormattedName,
    double TotalDistanceKm,
    double TotalWeightKg,
    double TotalVolumeM3,
    string ConsolidatedVehicleRequirement,
    FinancialScenarioDto FinancialScenario);

/// <summary>Best bid with the owning carrier's name for the financial scenario card.</summary>
public record BestBidDto(
    decimal Value,
    string CarrierName);

/// <summary>Bid metrics for the detail screen, including the winning carrier.</summary>
public record DetailBidMetricsDto(
    int TotalBids,
    BestBidDto? BestBid);

/// <summary>A single chronological stop of the Milking Run travel plan.</summary>
public record TravelPlanStopDto(
    int Order,
    string CityState,
    string ActionType,
    DateTimeOffset Deadline);

/// <summary>A linked segment (Trecho) summarized for the detail screen.</summary>
public record AuctionSegmentDto(
    Guid Id,
    string MainProduct,
    string Origin,
    string Destination,
    decimal? FinancialCeiling);

/// <summary>Full auction detail returned by GET /api/auctions/{id}.</summary>
public record AuctionDetailDto(
    Guid Id,
    string Status,
    DateTimeOffset ExpiresAt,
    AuctionDetailRouteDto Route,
    DetailBidMetricsDto BidMetrics,
    IReadOnlyList<TravelPlanStopDto> TravelPlan,
    IReadOnlyList<AuctionSegmentDto> Segments);

// ---------------------------------------------------------------------------
// Auction update / delete — PUT & DELETE /api/auctions/{id}
// ---------------------------------------------------------------------------

/// <summary>
/// Request body for editing the auction's custom name and/or bid deadline. Both fields
/// are optional; only the ones provided are updated.
/// </summary>
public record UpdateAuctionRequestDto(
    string? Name,
    DateTimeOffset? ExpiresAt);

/// <summary>Auction snapshot returned after a successful edit.</summary>
public record UpdateAuctionResponseDto(
    Guid Id,
    string? Name,
    DateTimeOffset ExpiresAt);
