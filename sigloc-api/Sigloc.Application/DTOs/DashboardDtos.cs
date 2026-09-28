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
