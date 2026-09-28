using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class DashboardService : IDashboardService
{
    private const int TopItems = 4;
    private const int CriticalThresholdMinutes = 60;

    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardExecutivoDto> GetExecutiveDashboardAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStartUtc = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Independent reads run in parallel to keep the aggregator (BFF) fast.
        var kpisTask = _dashboardRepository.GetKpisAsync(contractorId, monthStartUtc, cancellationToken);
        var occupationsTask = _dashboardRepository.GetInTransitOccupationsAsync(contractorId, cancellationToken);
        var deviationsTask = _dashboardRepository.GetCostDeviationsAsync(contractorId, cancellationToken);
        var milestonesTask = _dashboardRepository.GetSlaMilestonesAsync(contractorId, cancellationToken);

        await Task.WhenAll(kpisTask, occupationsTask, deviationsTask, milestonesTask);

        var kpis = kpisTask.Result;

        return new DashboardExecutivoDto(
            Kpis: new DashboardKpisDto(
                kpis.UnassignedSegments,
                kpis.ActiveAuctions,
                kpis.InTransitTrips,
                kpis.BlockedOverbookings),
            NetworkEfficiency: BuildEfficiency(occupationsTask.Result),
            CostDeviations: BuildCostDeviations(deviationsTask.Result),
            SlaMilestones: BuildSlaMilestones(milestonesTask.Result, now));
    }

    private static NetworkEfficiencyDto BuildEfficiency(IReadOnlyList<TripOccupation> occupations)
    {
        if (occupations.Count == 0)
        {
            return new NetworkEfficiencyDto(0, 0, 0);
        }

        var weight = occupations
            .Where(o => o.VehicleWeightCapacity > 0)
            .Select(o => Clamp(o.RouteWeightKg / o.VehicleWeightCapacity * 100))
            .DefaultIfEmpty(0)
            .Average();

        var volume = occupations
            .Where(o => o.VehicleVolumeCapacity > 0)
            .Select(o => Clamp(o.RouteVolumeM3 / o.VehicleVolumeCapacity * 100))
            .DefaultIfEmpty(0)
            .Average();

        // Continuous Move: share of in-transit trips whose route consolidates 2+ segments.
        var continuousMove = (double)occupations.Count(o => o.SegmentCount >= 2) / occupations.Count * 100;

        return new NetworkEfficiencyDto(
            Round(weight),
            Round(volume),
            Round(continuousMove));
    }

    private static IReadOnlyList<CostDeviationDto> BuildCostDeviations(IReadOnlyList<AuctionCostDeviation> deviations)
    {
        return deviations
            .Select(d =>
            {
                var deviation = d.CurrentBestBid - d.TargetBudget;
                return new CostDeviationDto(
                    RouteId: ToRouteCode(d.RouteId),
                    Itinerary: d.Itinerary,
                    TargetBudget: d.TargetBudget,
                    CurrentBestBid: d.CurrentBestBid,
                    DeviationAmount: deviation,
                    IsOverBudget: deviation > 0);
            })
            .OrderByDescending(d => Math.Abs(d.DeviationAmount))
            .Take(TopItems)
            .ToList();
    }

    private static IReadOnlyList<SlaMilestoneDto> BuildSlaMilestones(
        IReadOnlyList<TripSlaMilestone> milestones,
        DateTimeOffset now)
    {
        return milestones
            .Select(m =>
            {
                // Fall back to "now" when the trip has no ETA snapshot yet.
                var reference = m.LastCalculatedEta ?? now;
                var remaining = (long)Math.Round((m.SlaDeadline - reference).TotalMinutes);
                return new SlaMilestoneDto(
                    ReferenceCode: m.ReferenceCode,
                    Itinerary: m.Itinerary,
                    MilestoneType: m.MilestoneType,
                    TimeRemainingMinutes: remaining,
                    IsCritical: remaining < CriticalThresholdMinutes);
            })
            .OrderBy(m => m.TimeRemainingMinutes)
            .Take(TopItems)
            .ToList();
    }

    public async Task<DashboardTransportadorDto> GetCarrierDashboardAsync(
        Guid carrierId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStartUtc = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Independent reads run in parallel to keep the aggregator (BFF) fast.
        var kpisTask = _dashboardRepository.GetCarrierKpisAsync(carrierId, cancellationToken);
        var performanceTask = _dashboardRepository.GetCarrierPerformanceInputsAsync(carrierId, monthStartUtc, cancellationToken);
        var occupationsTask = _dashboardRepository.GetInTransitOccupationsByCarrierAsync(carrierId, cancellationToken);
        var disputesTask = _dashboardRepository.GetActiveBidDisputesAsync(carrierId, cancellationToken);
        var milestonesTask = _dashboardRepository.GetCarrierSlaMilestonesAsync(carrierId, cancellationToken);

        await Task.WhenAll(kpisTask, performanceTask, occupationsTask, disputesTask, milestonesTask);

        var kpis = kpisTask.Result;

        return new DashboardTransportadorDto(
            Kpis: new CarrierKpisDto(
                kpis.AvailableVehicles,
                kpis.ActiveBids,
                kpis.InTransitTrips),
            Performance: BuildPerformance(performanceTask.Result, occupationsTask.Result),
            ActiveDisputes: BuildActiveDisputes(disputesTask.Result),
            ControlTower: BuildControlTower(milestonesTask.Result, now));
    }

    private static CarrierPerformanceDto BuildPerformance(
        CarrierPerformanceInputs inputs,
        IReadOnlyList<TripOccupation> occupations)
    {
        var fleetOperation = inputs.TotalVehicles > 0
            ? (double)inputs.BusyVehicles / inputs.TotalVehicles * 100
            : 0;

        var capacityUtilization = BuildCapacityUtilization(occupations);

        var auctionSuccess = inputs.SubmittedBids > 0
            ? (double)inputs.WonBids / inputs.SubmittedBids * 100
            : 0;

        return new CarrierPerformanceDto(
            (decimal)Round(Clamp(fleetOperation)),
            (decimal)Round(Clamp(capacityUtilization)),
            (decimal)Round(Clamp(auctionSuccess)));
    }

    private static double BuildCapacityUtilization(IReadOnlyList<TripOccupation> occupations)
    {
        if (occupations.Count == 0)
        {
            return 0;
        }

        // Average of the weight and volume occupation ratios across the in-transit trips.
        var ratios = occupations
            .SelectMany(o => new[]
            {
                o.VehicleWeightCapacity > 0 ? Clamp(o.RouteWeightKg / o.VehicleWeightCapacity * 100) : (double?)null,
                o.VehicleVolumeCapacity > 0 ? Clamp(o.RouteVolumeM3 / o.VehicleVolumeCapacity * 100) : (double?)null
            })
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .ToList();

        return ratios.Count == 0 ? 0 : ratios.Average();
    }

    private static IReadOnlyList<ActiveDisputeDto> BuildActiveDisputes(IReadOnlyList<ActiveBidDispute> disputes)
    {
        return disputes
            .Select(d =>
            {
                var isWinning = d.MyBidAmount == d.LeaderBidAmount;
                var amountToCover = isWinning ? 0m : d.MyBidAmount - d.LeaderBidAmount;
                return new ActiveDisputeDto(
                    RouteId: ToRouteCode(d.RouteId),
                    Itinerary: d.Itinerary,
                    Status: isWinning ? "VENCENDO" : "PERDENDO",
                    MyBidAmount: d.MyBidAmount,
                    LeaderBidAmount: d.LeaderBidAmount,
                    AmountToCover: amountToCover);
            })
            // Closest to the lead first so the carrier can cover the cheapest gaps immediately.
            .OrderBy(d => d.AmountToCover)
            .Take(TopItems)
            .ToList();
    }

    private static IReadOnlyList<ControlTowerItemDto> BuildControlTower(
        IReadOnlyList<CarrierSlaMilestone> milestones,
        DateTimeOffset now)
    {
        return milestones
            .Select(m =>
            {
                // Fall back to "now" when the trip has no ETA snapshot yet.
                var reference = m.LastCalculatedEta ?? now;
                var remaining = (long)Math.Round((m.SlaDeadline - reference).TotalMinutes);
                return new ControlTowerItemDto(
                    VehiclePlate: m.VehiclePlate,
                    ReferenceCode: m.ReferenceCode,
                    MilestoneType: m.MilestoneType,
                    TimeRemainingMinutes: remaining,
                    IsDelayed: remaining < 0);
            })
            .OrderBy(m => m.TimeRemainingMinutes)
            .Take(TopItems)
            .ToList();
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);

    private static double Round(double value) => Math.Round(value, 1);

    private static string ToRouteCode(Guid routeId)
        => "ROT-" + routeId.ToString("N")[..6].ToUpperInvariant();
}
