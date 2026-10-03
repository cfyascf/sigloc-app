using System.Text.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Tests;

public class TripMonitoringServiceTests
{
    internal static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, true)] [InlineData(14.999, true)] [InlineData(15, false)] [InlineData(16, false)]
    public async Task Cache_uses_success_time_and_exact_boundary(double minutes, bool hit)
    {
        var h = new Harness(); h.Cached(minutes);
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal(!hit, result.IsCacheRenewed);
        Assert.Equal(hit ? 0 : 1, h.Tracking.Calls);
        Assert.Equal(hit ? 0 : 1, h.Store.RefreshCalls);
    }

    [Fact]
    public async Task First_view_routes_every_remaining_stop_and_updates_next_stop_eta()
    {
        var h = new Harness();
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.True(result.IsCacheRenewed);
        Assert.Equal(3, h.Routing.Coordinates!.Count);
        Assert.Equal(Now.AddMinutes(40), result.NewEta);
        Assert.Equal(60, result.ProgressPercentage);
        Assert.Equal(150, result.UtilizationPercentage);
        Assert.Single(h.Store.State!.NewTelemetry);
        Assert.Equal("AGUARDANDO_COLETA", result.Status);
    }

    [Fact]
    public async Task Origin_geofence_starts_trip_then_final_stop_finishes_without_routing()
    {
        var h = new Harness(); h.Tracking.Fix = h.Tracking.Fix with { Latitude = 0, Longitude = 0 };
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal("EM_TRANSITO", result.Status);
        Assert.Equal("CONCLUIDA", result.Stops[0].Status);
        Assert.Equal(SegmentStatus.InTransit, h.Store.State!.Segments[0].Status);
        Assert.Equal(RouteStatus.InTransit, h.Store.State.Route.Status);
        Assert.Equal(Now, h.Store.State.Trip.StartedAt);
        h.Clock.Now = Now.AddMinutes(16);
        h.Tracking.Fix = new TrackingFix("second", 1, 1, 1, h.Clock.Now);
        result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal("ENTREGUE", result.Status);
        Assert.Null(result.NewEta);
        Assert.Equal(100, result.ProgressPercentage);
        Assert.Equal(1, h.Routing.Calls);
        Assert.Equal(SegmentStatus.Completed, h.Store.State!.Segments[0].Status);
        Assert.Equal(RouteStatus.Completed, h.Store.State.Route.Status);
        Assert.Equal(OperationalStatus.LIVRE, h.Store.State.Vehicle.Status);
    }

    [Fact]
    public async Task One_observation_completes_only_next_stop_even_when_geofences_overlap()
    {
        var h = new Harness();
        h.State.Trip.Stops[1].Latitude = 0.0001; h.State.Trip.Stops[1].Longitude = 0;
        h.Tracking.Fix = h.Tracking.Fix with { Latitude = 0, Longitude = 0 };
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal("CONCLUIDA", result.Stops[0].Status);
        Assert.Equal("EM_TRANSITO", result.Stops[1].Status);
        Assert.Equal("EM_TRANSITO", result.Status);
        h.Clock.Now = Now.AddMinutes(16);
        h.Tracking.Fix = h.Tracking.Fix with { FixTime = h.Clock.Now };
        result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.True(result.IsStale);
        Assert.Equal("EM_TRANSITO", result.Stops[1].Status);
    }

    [Theory]
    [InlineData(39, "CRITIC")] [InlineData(40, "NORMAL")] [InlineData(41, "NORMAL")]
    public async Task Eta_compares_strictly_to_next_action_deadline(int deadlineMinutes, string risk)
    {
        var h = new Harness(); h.State.Trip.Stops[0].Actions[0].Deadline = Now.AddMinutes(deadlineMinutes);
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal(risk, result.Risk);
        Assert.Equal(risk == "CRITIC" ? "ATRASADO" : "AGUARDANDO_COLETA", result.Status);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Provider_failure_falls_back_only_if_previous_snapshot_exists(bool cached)
    {
        var h = new Harness(); if (cached) h.Cached(16);
        h.Routing.Fail = true;
        if (!cached) await Assert.ThrowsAsync<TrackingUnavailableException>(() => h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id));
        else
        {
            var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
            Assert.True(result.IsStale); Assert.False(result.IsCacheRenewed);
            Assert.Equal(Now.AddMinutes(-16), result.LastUpdated);
        }
        Assert.Empty(h.Store.State!.NewTelemetry);
        Assert.All(h.Store.State.Trip.Stops, s => Assert.False(s.IsCompleted));
    }

    [Fact]
    public async Task Failed_routing_rolls_back_tentative_origin_geofence()
    {
        var h = new Harness(); h.Routing.Fail = true;
        h.Tracking.Fix = h.Tracking.Fix with { Latitude = 0, Longitude = 0 };
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id));
        Assert.Equal(TripStatus.AwaitingPickup, h.Store.State!.Trip.Status);
        Assert.All(h.Store.State.Trip.Stops, s => Assert.False(s.IsCompleted));
        Assert.Empty(h.Store.State.Events);
    }

    [Theory]
    [InlineData("old")] [InlineData("future")] [InlineData("coordinate")] [InlineData("device")]
    [InlineData("identity")] [InlineData("missing-device")]
    public async Task Invalid_fixes_never_write_or_route(string failure)
    {
        var h = new Harness();
        h.Tracking.Fix = failure switch
        {
            "old" => h.Tracking.Fix with { FixTime = Now.AddMinutes(-16) },
            "future" => h.Tracking.Fix with { FixTime = Now.AddSeconds(1) },
            "coordinate" => h.Tracking.Fix with { Latitude = double.NaN },
            "device" => h.Tracking.Fix with { DeviceId = 2 },
            "identity" => h.Tracking.Fix with { ObservationId = "" },
            _ => h.Tracking.Fix
        };
        if (failure == "missing-device") h.State.Vehicle.TraccarDeviceId = null;
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id));
        Assert.Equal(0, h.Routing.Calls); Assert.Null(h.Store.State!.Snapshot);
    }

    [Fact]
    public async Task Cancellation_is_propagated_not_stale_fallback()
    {
        var h = new Harness(); h.Cached(16);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id, cts.Token));
    }

    [Theory]
    [InlineData(TripStatus.Delivered)] [InlineData(TripStatus.Cancelled)]
    public async Task Terminal_trip_never_restarts_monitoring(TripStatus status)
    {
        var h = new Harness(); h.State.Trip.Status = status;
        await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.Equal(0, h.Tracking.Calls); Assert.Equal(0, h.Store.RefreshCalls);
    }

    [Fact]
    public async Task Tenant_missing_is_not_found_before_provider_access()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<TripNotFoundException>(() => h.Service.GetDetailAsync(Guid.NewGuid(), h.State.Trip.Id));
        Assert.Equal(0, h.Tracking.Calls);
    }

    [Fact]
    public async Task List_is_read_only_and_bounds_paging()
    {
        var h = new Harness();
        await h.Service.SearchAsync(h.CompanyId, new TripQueryDto(-1, 1000));
        Assert.Equal(1, h.Store.LastQuery!.Page); Assert.Equal(100, h.Store.LastQuery.PageSize);
        Assert.Equal(0, h.Store.RefreshCalls); Assert.Equal(0, h.Tracking.Calls); Assert.Equal(0, h.Routing.Calls);
    }

    [Fact]
    public async Task Persistence_retry_reuses_provider_results_and_writes_one_observation()
    {
        var h = new Harness(); h.Store.Retry = true;
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.True(result.IsCacheRenewed);
        Assert.Equal(1, h.Tracking.Calls); Assert.Equal(1, h.Routing.Calls);
        Assert.Single(h.Store.State!.NewTelemetry);
        Assert.Equal(2, h.Store.State.Events.Count);
    }

    [Fact]
    public async Task Freshness_is_rechecked_after_lock()
    {
        var h = new Harness(); h.Store.BeforeRefresh = s => s.Snapshot = new TripMonitoring { LastSuccessfulCalculationAt = Now };
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        Assert.False(result.IsCacheRenewed); Assert.Equal(0, h.Tracking.Calls);
    }

    [Fact]
    public async Task Contract_preserves_Portuguese_fields_and_unknown_values()
    {
        var h = new Harness(); h.State.Trip.Status = TripStatus.Cancelled;
        var result = await h.Service.GetDetailAsync(h.CompanyId, h.State.Trip.Id);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.Equal(h.State.Trip.Id, json.RootElement.GetProperty("viagemId").GetGuid());
        Assert.True(json.RootElement.TryGetProperty("isCacheRenovado", out _));
        Assert.True(json.RootElement.TryGetProperty("cacheDesatualizado", out _));
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("motorista").GetProperty("telefone").ValueKind);
        Assert.True(json.RootElement.TryGetProperty("saudeOperacao", out _));
        Assert.True(json.RootElement.TryGetProperty("contextoGeografico", out _));
        Assert.True(json.RootElement.TryGetProperty("linhaDoTempo", out _));
        Assert.False(json.RootElement.TryGetProperty("RouteId", out _));
    }

    internal sealed class Harness
    {
        public Guid CompanyId { get; } = Guid.NewGuid();
        public TripState State => Store.State!;
        public FakeStore Store { get; }
        public FakeTracking Tracking { get; } = new();
        public FakeRouting Routing { get; } = new();
        public FakeClock Clock { get; } = new();
        public TripMonitoringService Service { get; }
        public Harness()
        {
            var route = new ConsolidatedRoute { Id = Guid.NewGuid(), ContractorId = CompanyId, TotalDistanceKm = 100, TotalVolumeM3 = 30 };
            var segment = Segment(); segment.RouteId = route.Id;
            var vehicle = new Vehicle(Guid.NewGuid(), "ABC1D23", "Truck", 2, 1000, 20, default, default, false, false, "Driver", "City")
                { Id = Guid.NewGuid(), TraccarDeviceId = 1 };
            var trip = new Trip { Id = Guid.NewGuid(), RouteId = route.Id, VehicleId = vehicle.Id };
            trip.Stops = TripItineraryBuilder.Build(trip.Id, new[] { segment });
            Store = new FakeStore(new TripState(trip, route, vehicle, new Carrier { Cnpj = "123", CompanyName = "Carrier" }, new() { segment }, null, new()));
            Service = new TripMonitoringService(Store, Tracking, Routing, Clock, new TripMonitoringOptions());
        }
        public void Cached(double minutes) => State.Snapshot = new TripMonitoring
        {
            Id = Guid.NewGuid(), TripId = State.Trip.Id, LastSuccessfulCalculationAt = Now.AddMinutes(-minutes),
            LastPingAt = Now.AddMinutes(-20), Risk = "NORMAL", LastCalculatedEta = Now.AddHours(2)
        };
    }
    internal static RouteSegment Segment() => new()
    {
        Id = Guid.NewGuid(), OriginAddress = "Origin, AA", DestinationAddress = "Destination, BB",
        OriginCoordinate = "0,0", DestinationCoordinate = "1,1", PickupDeadline = Now.AddHours(1),
        DeliveryDeadline = Now.AddHours(3), Status = SegmentStatus.Routed, RouteSequence = 1
    };
    internal sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = TripMonitoringServiceTests.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    internal sealed class FakeTracking : ITripTrackingProvider
    {
        public int Calls { get; private set; }
        public TrackingFix Fix { get; set; } = new("one", 1, 2, 2, Now);
        public Task<TrackingFix> GetLatestAsync(long deviceId, DateTimeOffset now, CancellationToken ct = default)
        { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(Fix); }
    }
    internal sealed class FakeRouting : ITripRoutingProvider
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public IReadOnlyList<RouteCoordinate>? Coordinates { get; private set; }
        public Task<TripRouteEstimate> CalculateAsync(IReadOnlyList<RouteCoordinate> coordinates, CancellationToken ct = default)
        {
            Calls++; Coordinates = coordinates;
            if (Fail) throw new TrackingUnavailableException();
            return Task.FromResult(new TripRouteEstimate(40, 600));
        }
    }
    internal sealed class FakeStore(TripState state) : ITripMonitoringStore
    {
        public TripState? State { get; set; } = state;
        public int RefreshCalls { get; private set; }
        public bool Retry { get; set; }
        public Action<TripState>? BeforeRefresh { get; set; }
        public TripQueryDto? LastQuery { get; private set; }
        public Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct)
        { LastQuery = query; return Task.FromResult(new PagedTripsDto([], query.Page, query.PageSize, 0, 0)); }
        public Task<TripState?> ReadAsync(Guid contractorId, Guid tripId, CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); return Task.FromResult(State?.Route.ContractorId == contractorId && State.Trip.Id == tripId ? State : null); }
        public async Task<TripDetailDto> RefreshAsync(Guid contractorId, Guid tripId, Func<TripState, CancellationToken, Task<TripDetailDto>> refresh, CancellationToken ct)
        {
            RefreshCalls++;
            TripState Clone() => JsonSerializer.Deserialize<TripState>(JsonSerializer.Serialize(State))!;
            var attempt = Clone(); BeforeRefresh?.Invoke(attempt);
            var result = await refresh(attempt, ct);
            if (Retry) { attempt = Clone(); result = await refresh(attempt, ct); }
            State = attempt;
            return result;
        }
    }
}
