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
/// Request body for editing the auction's bid deadline. The field is optional; the
/// deadline is only updated when it is provided.
/// </summary>
public record UpdateAuctionRequestDto(
    DateTimeOffset? ExpiresAt);

/// <summary>Auction snapshot returned after a successful edit.</summary>
public record UpdateAuctionResponseDto(
    Guid Id,
    DateTimeOffset ExpiresAt);

// ---------------------------------------------------------------------------
// Bid ranking (Análise de Lances) — GET /api/auctions/{id}/bids
// ---------------------------------------------------------------------------

/// <summary>Route summary block shown above the bid ranking.</summary>
public record BidRankingRouteSummaryDto(
    Guid RouteId,
    string ItinerarySummary,
    string ItineraryWithStates,
    int SegmentCount,
    Guid? FirstSegmentId,
    IReadOnlyList<Guid> LinkedSegmentIds,
    DateTimeOffset? FirstPickupDeadline,
    DateTimeOffset? LastDeliveryDeadline,
    FinancialScenarioDto FinancialScenario);

/// <summary>Carrier reliability data shown on each ranking row.</summary>
public record BidRankingCarrierDto(
    Guid Id,
    string TradeName,
    double? AverageRating,
    int OnTimeDeliveryRate,
    bool HasActiveInsurancePolicy);

/// <summary>Vehicle data shown on each ranking row.</summary>
public record BidRankingVehicleDto(
    string Plate,
    string BodyType);

/// <summary>Financial breakdown and savings of a ranked bid.</summary>
public record BidRankingFinancialsDto(
    decimal TotalValue,
    decimal NetFreightValue,
    decimal TollValue,
    decimal SavingsValue,
    double SavingsPercentage);

/// <summary>A single ranked bid row.</summary>
public record BidRankingItemDto(
    int Rank,
    Guid BidId,
    string Status,
    DateTimeOffset SubmittedAt,
    BidRankingCarrierDto Carrier,
    BidRankingVehicleDto Vehicle,
    BidRankingFinancialsDto Financials);

/// <summary>Full response for the bid ranking endpoint.</summary>
public record BidRankingDto(
    Guid AuctionId,
    string Status,
    BidRankingRouteSummaryDto Route,
    IReadOnlyList<BidRankingItemDto> Bids);

// ---------------------------------------------------------------------------
// Award (Adjudicação) — POST /api/auctions/{id}/award
// ---------------------------------------------------------------------------

/// <summary>Request body for awarding an auction to the winning bid.</summary>
public record AwardAuctionRequestDto(
    Guid WinningBidId);

/// <summary>Response returned after a successful award, carrying the created trip id.</summary>
public record AwardAuctionResponseDto(
    Guid TripId,
    Guid AuctionId,
    Guid WinningBidId,
    string AuctionStatus,
    string RouteStatus);

// ---------------------------------------------------------------------------
// Carrier bid workspace (Workspace de Lance) — GET /api/auctions/{id}/carrier-analysis
// ---------------------------------------------------------------------------

/// <summary>SLA window of the consolidated route: first pickup and last delivery deadlines.</summary>
public record CarrierAnalysisSlaDto(
    DateTimeOffset? FirstPickup,
    DateTimeOffset? LastDelivery);

/// <summary>Short route summary shown at the top of the carrier bid workspace.</summary>
public record CarrierAnalysisRouteSummaryDto(
    string ReferenceCode,
    string ShortItinerary,
    CarrierAnalysisSlaDto Sla);

/// <summary>Competition snapshot: active bids, leader offer and auction ceiling.</summary>
public record CarrierAnalysisCompetitionDto(
    int ActiveBids,
    decimal? BestLeaderOffer,
    decimal AuctionCeiling);

/// <summary>Consolidated physical/compliance requirements inherited from the cargo.</summary>
public record CarrierAnalysisPhysicalRequirementsDto(
    string RecommendedFleet,
    double ConsolidatedWeightKg,
    double VolumeM3,
    string RequiredTemperature,
    IReadOnlyList<string> HandlingRestrictions);

/// <summary>A single vehicle of the carrier's fleet offered in the bid dropdown.</summary>
public record CarrierAnalysisVehicleDto(
    Guid VehicleId,
    string Plate,
    string Model,
    decimal CapacityWeightKg,
    decimal CapacityVolumeM3,
    IReadOnlyList<string> Specifications);

/// <summary>A single stop of the Milking Run travel plan for the carrier workspace.</summary>
public record CarrierAnalysisTravelStopDto(
    int Order,
    string City,
    string Action);

/// <summary>
/// The carrier's own current bid on the auction, when one exists. Lets the workspace
/// pre-fill the proposal value and highlight the active bid instead of showing the ceiling.
/// </summary>
public record CarrierAnalysisMyBidDto(
    Guid BidId,
    Guid VehicleId,
    decimal NetFreightValue,
    decimal TollValue,
    decimal TotalValue,
    DateTimeOffset SubmittedAt,
    string Status);

/// <summary>Full carrier bid workspace returned by GET /api/auctions/{id}/carrier-analysis.</summary>
public record CarrierBidAnalysisDto(
    Guid AuctionId,
    CarrierAnalysisRouteSummaryDto RouteSummary,
    CarrierAnalysisCompetitionDto Competition,
    CarrierAnalysisPhysicalRequirementsDto PhysicalRequirements,
    IReadOnlyList<CarrierAnalysisVehicleDto> CarrierAvailableFleet,
    IReadOnlyList<CarrierAnalysisTravelStopDto> TravelPlan,
    CarrierAnalysisMyBidDto? MyBid);

// ---------------------------------------------------------------------------
// Place bid (Submissão do Lance) — POST /api/auctions/{id}/bids
// ---------------------------------------------------------------------------

/// <summary>Request body for a carrier submitting a bid against an auction.</summary>
public record PlaceBidRequestDto(
    decimal ValorOferecido,
    Guid VeiculoId);

/// <summary>Response returned after a successful bid submission (201 Created).</summary>
public record PlaceBidResponseDto(
    Guid BidId,
    Guid AuctionId,
    decimal NetFreightValue,
    decimal TollValue,
    decimal TotalValue,
    DateTimeOffset SubmittedAt,
    string Status);
