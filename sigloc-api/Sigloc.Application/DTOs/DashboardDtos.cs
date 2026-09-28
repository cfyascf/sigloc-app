namespace Sigloc.Application.DTOs;

/// <summary>
/// Executive dashboard payload (Dashboard Executivo). Aggregated server-side and returned
/// ready to render, scoped to the calling contractor.
/// </summary>
public record DashboardExecutivoDto(
    DashboardKpisDto Kpis,
    NetworkEfficiencyDto NetworkEfficiency,
    IReadOnlyList<CostDeviationDto> CostDeviations,
    IReadOnlyList<SlaMilestoneDto> SlaMilestones);

/// <summary>Global state counters shown at the top of the dashboard.</summary>
public record DashboardKpisDto(
    int UnassignedSegments,
    int ActiveAuctions,
    int InTransitTrips,
    int BlockedOverbookings);

/// <summary>Average fleet occupation and route utilization for the current in-transit trips.</summary>
public record NetworkEfficiencyDto(
    double AverageWeightOccupationPercentage,
    double AverageVolumeOccupationPercentage,
    double RouteUtilizationPercentage);

/// <summary>
/// One of the top cost-deviation auctions. <see cref="DeviationAmount"/> is
/// <c>CurrentBestBid - TargetBudget</c>; positive means over budget (rendered red).
/// </summary>
public record CostDeviationDto(
    string RouteId,
    string Itinerary,
    decimal TargetBudget,
    decimal CurrentBestBid,
    decimal DeviationAmount,
    bool IsOverBudget);

/// <summary>
/// One of the most critical SLA milestones. <see cref="TimeRemainingMinutes"/> is the ETA vs
/// deadline gap in minutes (may be negative when already late); critical below one hour.
/// </summary>
public record SlaMilestoneDto(
    string ReferenceCode,
    string Itinerary,
    string MilestoneType,
    long TimeRemainingMinutes,
    bool IsCritical);

/// <summary>
/// Carrier (transportador) dashboard payload. Aggregated server-side (BFF) and returned
/// ready to render, scoped to the calling carrier: immediate operational KPIs, monthly
/// performance metrics, the active-bid radar and the SLA control tower.
/// </summary>
public record DashboardTransportadorDto(
    CarrierKpisDto Kpis,
    CarrierPerformanceDto Performance,
    IReadOnlyList<ActiveDisputeDto> ActiveDisputes,
    IReadOnlyList<ControlTowerItemDto> ControlTower);

/// <summary>Immediate operational state counters shown at the top of the carrier dashboard.</summary>
public record CarrierKpisDto(
    int AvailableVehicles,
    int ActiveBids,
    int InTransitTrips);

/// <summary>Aggregated performance metrics for the current month.</summary>
public record CarrierPerformanceDto(
    decimal FleetOperationPercentage,
    decimal CapacityUtilizationPercentage,
    decimal AuctionSuccessRate);

/// <summary>
/// One of the top active-bid disputes. <see cref="Status"/> is <c>VENCENDO</c> when the
/// carrier holds the leader bid, otherwise <c>PERDENDO</c>; <see cref="AmountToCover"/> is
/// <c>MyBidAmount - LeaderBidAmount</c> (zero when winning).
/// </summary>
public record ActiveDisputeDto(
    string RouteId,
    string Itinerary,
    string Status,
    decimal MyBidAmount,
    decimal LeaderBidAmount,
    decimal AmountToCover);

/// <summary>
/// One of the most critical SLA milestones for the carrier's in-transit fleet.
/// <see cref="TimeRemainingMinutes"/> is the ETA vs deadline gap in minutes (negative when
/// already late); <see cref="IsDelayed"/> is true when the milestone is past due.
/// </summary>
public record ControlTowerItemDto(
    string VehiclePlate,
    string ReferenceCode,
    string MilestoneType,
    long TimeRemainingMinutes,
    bool IsDelayed);
