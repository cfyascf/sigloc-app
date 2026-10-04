using Sigloc.Application.Services;
using Sigloc.Domain.Repositories;

namespace Sigloc.Tests.Unit;

public class DashboardServiceTests
{
    private readonly IDashboardRepository _repository = Substitute.For<IDashboardRepository>();
    private readonly DashboardService _service;
    private readonly Guid _contractorId = Guid.NewGuid();
    private readonly Guid _carrierId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        _service = new DashboardService(_repository);
        StubExecutiveDefaults();
        StubCarrierDefaults();
    }

    private void StubExecutiveDefaults()
    {
        _repository.GetKpisAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new DashboardKpis(0, 0, 0));
        _repository.GetInTransitOccupationsAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<TripOccupation>());
        _repository.GetCostDeviationsAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<AuctionCostDeviation>());
        _repository.GetSlaMilestonesAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new List<TripSlaMilestone>());
    }

    private void StubCarrierDefaults()
    {
        _repository.GetCarrierKpisAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new CarrierDashboardKpis(0, 0, 0));
        _repository.GetCarrierPerformanceInputsAsync(_carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new CarrierPerformanceInputs(0, 0, 0, 0));
        _repository.GetInTransitOccupationsByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<TripOccupation>());
        _repository.GetActiveBidDisputesAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<ActiveBidDispute>());
        _repository.GetCarrierSlaMilestonesAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<CarrierSlaMilestone>());
    }

    // ---------------- Executive dashboard ----------------

    [Fact]
    public async Task GetExecutiveDashboardAsync_maps_kpis()
    {
        _repository.GetKpisAsync(_contractorId, Arg.Any<CancellationToken>())
            .Returns(new DashboardKpis(5, 3, 8));

        var result = await _service.GetExecutiveDashboardAsync(_contractorId);

        result.Kpis.UnassignedSegments.Should().Be(5);
        result.Kpis.ActiveAuctions.Should().Be(3);
        result.Kpis.InTransitTrips.Should().Be(8);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_empty_occupations_yield_zero_efficiency()
    {
        var result = await _service.GetExecutiveDashboardAsync(_contractorId);

        result.NetworkEfficiency.AverageWeightOccupationPercentage.Should().Be(0);
        result.NetworkEfficiency.AverageVolumeOccupationPercentage.Should().Be(0);
        result.NetworkEfficiency.RouteUtilizationPercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_efficiency_ignores_zero_capacity_and_clamps()
    {
        var occupations = new List<TripOccupation>
        {
            // Full occupation: 100% weight, 100% volume, 2 segments (continuous move).
            new(RouteWeightKg: 1000, VehicleWeightCapacity: 1000, RouteVolumeM3: 50, VehicleVolumeCapacity: 50, SegmentCount: 2),
            // Over capacity -> clamped to 100; single segment (no continuous move).
            new(RouteWeightKg: 2000, VehicleWeightCapacity: 1000, RouteVolumeM3: 100, VehicleVolumeCapacity: 50, SegmentCount: 1),
            // Zero capacity rows ignored in the weight/volume averages.
            new(RouteWeightKg: 500, VehicleWeightCapacity: 0, RouteVolumeM3: 25, VehicleVolumeCapacity: 0, SegmentCount: 1),
        };
        _repository.GetInTransitOccupationsAsync(_contractorId, Arg.Any<CancellationToken>()).Returns(occupations);

        var result = await _service.GetExecutiveDashboardAsync(_contractorId);

        // Only the two non-zero-capacity rows count: (100 + 100) / 2 = 100.
        result.NetworkEfficiency.AverageWeightOccupationPercentage.Should().Be(100);
        result.NetworkEfficiency.AverageVolumeOccupationPercentage.Should().Be(100);
        // 1 of 3 trips has >= 2 segments -> 33.3%.
        result.NetworkEfficiency.RouteUtilizationPercentage.Should().Be(33.3);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_cost_deviations_compute_flag_and_rank_top4()
    {
        var deviations = new List<AuctionCostDeviation>
        {
            new(Guid.NewGuid(), "A->B", TargetBudget: 100m, CurrentBestBid: 90m),   // dev -10, under budget
            new(Guid.NewGuid(), "C->D", TargetBudget: 100m, CurrentBestBid: 160m),  // dev +60, over budget (largest abs)
            new(Guid.NewGuid(), "E->F", TargetBudget: 100m, CurrentBestBid: 120m),  // dev +20
            new(Guid.NewGuid(), "G->H", TargetBudget: 100m, CurrentBestBid: 70m),   // dev -30
            new(Guid.NewGuid(), "I->J", TargetBudget: 100m, CurrentBestBid: 101m),  // dev +1 (smallest abs -> dropped)
        };
        _repository.GetCostDeviationsAsync(_contractorId, Arg.Any<CancellationToken>()).Returns(deviations);

        var result = await _service.GetExecutiveDashboardAsync(_contractorId);

        result.CostDeviations.Should().HaveCount(4);
        result.CostDeviations[0].Itinerary.Should().Be("C->D");
        result.CostDeviations[0].DeviationAmount.Should().Be(60m);
        result.CostDeviations[0].IsOverBudget.Should().BeTrue();
        result.CostDeviations.Should().Contain(d => d.Itinerary == "A->B" && d.IsOverBudget == false);
        result.CostDeviations.Should().NotContain(d => d.Itinerary == "I->J");
        result.CostDeviations.Should().OnlyContain(d => d.RouteId.StartsWith("ROT-"));
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_sla_milestones_critical_threshold_and_ordering()
    {
        var now = DateTimeOffset.UtcNow;
        var milestones = new List<TripSlaMilestone>
        {
            // 30 minutes remaining -> critical.
            new("REF-1", "A->B", "COLETA", SlaDeadline: now.AddMinutes(30), LastCalculatedEta: now),
            // 120 minutes remaining -> not critical.
            new("REF-2", "C->D", "ENTREGA", SlaDeadline: now.AddMinutes(120), LastCalculatedEta: now),
            // No ETA snapshot: falls back to now; deadline in 90 min -> not critical.
            new("REF-3", "E->F", "COLETA", SlaDeadline: now.AddMinutes(90), LastCalculatedEta: null),
        };
        _repository.GetSlaMilestonesAsync(_contractorId, Arg.Any<CancellationToken>()).Returns(milestones);

        var result = await _service.GetExecutiveDashboardAsync(_contractorId);

        result.SlaMilestones.Should().HaveCount(3);
        result.SlaMilestones[0].ReferenceCode.Should().Be("REF-1");
        result.SlaMilestones[0].IsCritical.Should().BeTrue();
        result.SlaMilestones.Single(m => m.ReferenceCode == "REF-2").IsCritical.Should().BeFalse();
        result.SlaMilestones.Should().BeInAscendingOrder(m => m.TimeRemainingMinutes);
    }

    // ---------------- Carrier dashboard ----------------

    [Fact]
    public async Task GetCarrierDashboardAsync_maps_kpis()
    {
        _repository.GetCarrierKpisAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new CarrierDashboardKpis(4, 6, 2));

        var result = await _service.GetCarrierDashboardAsync(_carrierId);

        result.Kpis.AvailableVehicles.Should().Be(4);
        result.Kpis.ActiveBids.Should().Be(6);
        result.Kpis.InTransitTrips.Should().Be(2);
    }

    [Fact]
    public async Task GetCarrierDashboardAsync_zero_inputs_yield_zero_performance()
    {
        var result = await _service.GetCarrierDashboardAsync(_carrierId);

        result.Performance.FleetOperationPercentage.Should().Be(0m);
        result.Performance.CapacityUtilizationPercentage.Should().Be(0m);
        result.Performance.AuctionSuccessRate.Should().Be(0m);
    }

    [Fact]
    public async Task GetCarrierDashboardAsync_performance_ratios_computed()
    {
        _repository.GetCarrierPerformanceInputsAsync(_carrierId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new CarrierPerformanceInputs(TotalVehicles: 10, BusyVehicles: 5, SubmittedBids: 4, WonBids: 1));
        _repository.GetInTransitOccupationsByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<TripOccupation>
            {
                new(RouteWeightKg: 500, VehicleWeightCapacity: 1000, RouteVolumeM3: 25, VehicleVolumeCapacity: 50, SegmentCount: 1),
            });

        var result = await _service.GetCarrierDashboardAsync(_carrierId);

        result.Performance.FleetOperationPercentage.Should().Be(50m);
        // weight 50% + volume 50% averaged -> 50%.
        result.Performance.CapacityUtilizationPercentage.Should().Be(50m);
        result.Performance.AuctionSuccessRate.Should().Be(25m);
    }

    [Fact]
    public async Task GetCarrierDashboardAsync_capacity_ignores_zero_capacity_rows()
    {
        _repository.GetInTransitOccupationsByCarrierAsync(_carrierId, Arg.Any<CancellationToken>())
            .Returns(new List<TripOccupation>
            {
                new(RouteWeightKg: 800, VehicleWeightCapacity: 1000, RouteVolumeM3: 10, VehicleVolumeCapacity: 0, SegmentCount: 1),
            });

        var result = await _service.GetCarrierDashboardAsync(_carrierId);

        // Only the weight ratio (80%) counts; the zero volume capacity is ignored.
        result.Performance.CapacityUtilizationPercentage.Should().Be(80m);
    }

    [Fact]
    public async Task GetCarrierDashboardAsync_disputes_winning_and_losing_ranked_top4()
    {
        var disputes = new List<ActiveBidDispute>
        {
            new(Guid.NewGuid(), "A->B", MyBidAmount: 100m, LeaderBidAmount: 100m), // winning -> 0 to cover
            new(Guid.NewGuid(), "C->D", MyBidAmount: 150m, LeaderBidAmount: 120m), // losing -> 30 to cover
            new(Guid.NewGuid(), "E->F", MyBidAmount: 200m, LeaderBidAmount: 190m), // losing -> 10 to cover
            new(Guid.NewGuid(), "G->H", MyBidAmount: 300m, LeaderBidAmount: 250m), // losing -> 50 to cover
            new(Guid.NewGuid(), "I->J", MyBidAmount: 400m, LeaderBidAmount: 300m), // losing -> 100 (dropped)
        };
        _repository.GetActiveBidDisputesAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(disputes);

        var result = await _service.GetCarrierDashboardAsync(_carrierId);

        result.ActiveDisputes.Should().HaveCount(4);
        result.ActiveDisputes[0].Itinerary.Should().Be("A->B");
        result.ActiveDisputes[0].Status.Should().Be("VENCENDO");
        result.ActiveDisputes[0].AmountToCover.Should().Be(0m);
        result.ActiveDisputes.Single(d => d.Itinerary == "C->D").Status.Should().Be("PERDENDO");
        result.ActiveDisputes.Single(d => d.Itinerary == "C->D").AmountToCover.Should().Be(30m);
        result.ActiveDisputes.Should().BeInAscendingOrder(d => d.AmountToCover);
        result.ActiveDisputes.Should().NotContain(d => d.Itinerary == "I->J");
    }

    [Fact]
    public async Task GetCarrierDashboardAsync_control_tower_delayed_flag_and_ordering()
    {
        var now = DateTimeOffset.UtcNow;
        var milestones = new List<CarrierSlaMilestone>
        {
            // Already late: deadline 30 min before ETA -> delayed.
            new("ABC1D23", "REF-1", "ENTREGA", SlaDeadline: now.AddMinutes(-30), LastCalculatedEta: now),
            // On time.
            new("XYZ9Z99", "REF-2", "COLETA", SlaDeadline: now.AddMinutes(90), LastCalculatedEta: now),
            // No ETA snapshot: falls back to now; deadline in 10 min -> on time.
            new("JKL4K44", "REF-3", "COLETA", SlaDeadline: now.AddMinutes(10), LastCalculatedEta: null),
        };
        _repository.GetCarrierSlaMilestonesAsync(_carrierId, Arg.Any<CancellationToken>()).Returns(milestones);

        var result = await _service.GetCarrierDashboardAsync(_carrierId);

        result.ControlTower.Should().HaveCount(3);
        result.ControlTower[0].ReferenceCode.Should().Be("REF-1");
        result.ControlTower[0].VehiclePlate.Should().Be("ABC1D23");
        result.ControlTower[0].IsDelayed.Should().BeTrue();
        result.ControlTower.Single(m => m.ReferenceCode == "REF-2").IsDelayed.Should().BeFalse();
        result.ControlTower.Should().BeInAscendingOrder(m => m.TimeRemainingMinutes);
    }
}
