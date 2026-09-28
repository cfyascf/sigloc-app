namespace Sigloc.Domain.Repositories;

/// <summary>Global state counters shown at the top of the executive dashboard.</summary>
public sealed record DashboardKpis(
    int UnassignedSegments,
    int ActiveAuctions,
    int InTransitTrips,
    int BlockedOverbookings);

/// <summary>
/// Per-trip occupation snapshot used to compute the network efficiency averages. Occupation
/// ratios are the consolidated route totals over the winning vehicle capacities. <see cref="SegmentCount"/>
/// feeds the Continuous Move (route utilization) indicator.
/// </summary>
public sealed record TripOccupation(
    double RouteWeightKg,
    double VehicleWeightCapacity,
    double RouteVolumeM3,
    double VehicleVolumeCapacity,
    int SegmentCount);

/// <summary>
/// A contractor auction with its target budget (consolidated ceiling) and the current best
/// (lowest total) bid. Only auctions that already have at least one active bid are returned.
/// </summary>
public sealed record AuctionCostDeviation(
    Guid RouteId,
    string Itinerary,
    decimal TargetBudget,
    decimal CurrentBestBid);

/// <summary>
/// An in-transit trip milestone with the data needed to compute the SLA countdown: the SLA
/// deadline of the next relevant segment leg and the last ETA calculated by the routing engine
/// (null when no monitoring snapshot exists, in which case the caller falls back to "now").
/// </summary>
public sealed record TripSlaMilestone(
    string ReferenceCode,
    string Itinerary,
    string MilestoneType,
    DateTimeOffset SlaDeadline,
    DateTimeOffset? LastCalculatedEta);

/// <summary>
/// Read-only aggregations backing the executive dashboard (BFF). Every query is
/// <c>AsNoTracking()</c> and scoped to the calling contractor.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Counts the contractor's available segments, open auctions, in-transit trips and blocked attempts this month.</summary>
    Task<DashboardKpis> GetKpisAsync(Guid contractorId, DateTimeOffset monthStartUtc, CancellationToken cancellationToken = default);

    /// <summary>Returns the occupation snapshot for every in-transit trip of the contractor.</summary>
    Task<IReadOnlyList<TripOccupation>> GetInTransitOccupationsAsync(Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>Returns the contractor's active auctions (with at least one bid) and their best bid, for the cost-deviation ranking.</summary>
    Task<IReadOnlyList<AuctionCostDeviation>> GetCostDeviationsAsync(Guid contractorId, CancellationToken cancellationToken = default);

    /// <summary>Returns the SLA milestone rows for the contractor's in-transit trips, for the SLA countdown ranking.</summary>
    Task<IReadOnlyList<TripSlaMilestone>> GetSlaMilestonesAsync(Guid contractorId, CancellationToken cancellationToken = default);
}
