using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sigloc.Application.Services;

public sealed class TripMonitoringService(
    ITripMonitoringStore store, ITripTrackingProvider tracking, ITripRoutingProvider routing,
    TimeProvider clock, TripMonitoringOptions options, ILogger<TripMonitoringService>? logger = null) : ITripMonitoringService
{
    private readonly TripMonitoringOptions _settings = options;
    private readonly ILogger _logger = logger ?? NullLogger<TripMonitoringService>.Instance;

    public Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct = default) =>
        SearchAsync(TripScope.ForShipper(contractorId), query, ct);

    public Task<TripDetailDto> GetDetailAsync(Guid contractorId, Guid tripId, bool refresh = false, CancellationToken ct = default) =>
        GetDetailAsync(TripScope.ForShipper(contractorId), tripId, refresh, (_, detail) => detail, ct);

    public Task<PagedTripsDto> SearchAsync(TripScope scope, TripQueryDto query, CancellationToken ct = default)
    {
        var status = Normalize(query.Status);
        var risk = Normalize(query.Risk);
        if (status != null && status is not ("AGUARDANDO_COLETA" or "EM_TRANSITO" or "ATRASADO")) Invalid("status");
        if (risk != null && risk is not ("NORMAL" or "CRITIC" or "NAO_MONITORADO")) Invalid("risk");
        var size = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var page = Math.Max(query.Page, 1);
        if ((long)(page - 1) * size > int.MaxValue) Invalid("page");
        return store.SearchAsync(scope, query with { Page = page, PageSize = size, Search = query.Search?.Trim(), Status = status, Risk = risk }, ct);
    }

    public async Task<T> GetDetailAsync<T>(TripScope scope, Guid tripId, bool refresh,
        Func<TripState, TripDetailDto, T> project, CancellationToken ct = default)
    {
        T Project(TripState state, bool renewed, bool stale = false) => project(state, TripDetailMapper.Map(state, renewed, stale));
        var previous = await store.ReadAsync(scope, tripId, ct) ?? throw new TripNotFoundException(tripId);
        // GPS/routing providers are only contacted on an explicit user-triggered refresh
        // ("Locate driver" button). A plain page load returns the last stored snapshot so
        // simply viewing a trip never consumes the tracking provider quota.
        if (!refresh || TripMonitoringCalculator.IsTerminal(previous.Trip) || TripMonitoringCalculator.IsFresh(previous.Snapshot, clock.GetUtcNow()))
            return Project(previous, false);

        // Memoize provider work across EF retries; each attempt receives a new tracked graph.
        TrackingFix? fix = null;
        var estimates = new Dictionary<string, TripRouteEstimate>();
        var refreshId = Guid.NewGuid();
        try
        {
            return await store.UpdateAsync(scope, tripId, async (state, token) =>
            {
                var now = clock.GetUtcNow();
                if (TripMonitoringCalculator.IsTerminal(state.Trip) || TripMonitoringCalculator.IsFresh(state.Snapshot, now))
                    return Project(state, false);
                TripStopCompletion.EnsureItinerary(state);
                if (state.Trip.Stops.Count == 0 || state.Vehicle.TraccarDeviceId is not > 0)
                    throw new TrackingUnavailableException();
                fix ??= await tracking.GetLatestAsync(state.Vehicle.TraccarDeviceId.Value, now, token);
                ValidateFix(fix, state, now);
                var pending = TripStopCompletion.Pending(state.Trip);
                var reached = pending.FirstOrDefault();
                if (reached != null && TripMonitoringCalculator.DistanceMeters(fix.Latitude, fix.Longitude, reached.Latitude, reached.Longitude) > _settings.GeofenceRadiusMeters)
                    reached = null;
                var remaining = pending.Where(s => s != reached).ToList();
                var next = remaining.FirstOrDefault();
                TripRouteEstimate estimate;
                if (remaining.Count == 0) estimate = new TripRouteEstimate(0, 0);
                else
                {
                    var coordinates = new[] { new RouteCoordinate(fix.Latitude, fix.Longitude) }
                        .Concat(remaining.Select(s => new RouteCoordinate(s.Latitude, s.Longitude))).ToList();
                    var key = string.Join(";", coordinates.Select(c => FormattableString.Invariant($"{c.Latitude:R},{c.Longitude:R}")));
                    if (!estimates.TryGetValue(key, out estimate!))
                    {
                        estimate = await routing.CalculateAsync(coordinates, token);
                        estimates.Add(key, estimate);
                    }
                }
                if (!double.IsFinite(estimate.RemainingDistanceKm) || estimate.RemainingDistanceKm < 0
                    || !double.IsFinite(estimate.NextStopDurationSeconds) || estimate.NextStopDurationSeconds < 0)
                    throw new TrackingUnavailableException();
                var deadline = next?.Actions.Where(a => !a.IsCompleted).Min(a => (DateTimeOffset?)a.Deadline);
                DateTimeOffset? eta = next == null ? null : now.AddSeconds(estimate.NextStopDurationSeconds).AddMinutes(_settings.DockBufferMinutes);
                var risk = eta > deadline ? "CRITIC" : "NORMAL";
                var finished = next == null;
                var (traveled, progress) = TripMonitoringCalculator.Progress(state.Route.TotalDistanceKm, estimate.RemainingDistanceKm, finished);

                // All provider results and calculations succeeded; mutations are now committed atomically.
                if (reached != null)
                {
                    TripStopCompletion.Complete(state, reached, fix.FixTime, _ => "GEOFENCE_INFERRED");
                    TripStopCompletion.AddEvent(state, refreshId, "PARADA_INFERIDA", $"Entrada na cerca da parada {reached.Sequence}; coleta/entrega inferida, não verificada.", now);
                }
                if (finished) TripStopCompletion.Finish(state, fix.FixTime);
                var snapshot = state.Snapshot ??= new TripMonitoring { Id = Guid.NewGuid(), TripId = state.Trip.Id };
                if (snapshot.Risk != risk) TripStopCompletion.AddEvent(state, refreshId, "SLA", risk == "CRITIC" ? "ETA calculada excede o prazo da próxima parada." : "SLA recalculado sem atraso previsto.", now);
                snapshot.LastSuccessfulCalculationAt = now;
                snapshot.LastPingAt = fix.FixTime; snapshot.LastObservationId = fix.ObservationId; snapshot.LastDeviceId = fix.DeviceId;
                snapshot.Latitude = fix.Latitude; snapshot.Longitude = fix.Longitude;
                snapshot.LastProgressPercentage = progress; snapshot.TraveledDistanceKm = traveled;
                snapshot.RemainingDistanceKm = estimate.RemainingDistanceKm; snapshot.LastCalculatedEta = eta;
                snapshot.NextStopId = next?.Id; snapshot.NextStopDeadline = deadline; snapshot.Risk = risk;
                state.NewTelemetry.Add(new TripTelemetry { Id = refreshId, TripId = state.Trip.Id, ObservationId = fix.ObservationId,
                    DeviceId = fix.DeviceId, Latitude = fix.Latitude, Longitude = fix.Longitude, FixTime = fix.FixTime, CalculatedAt = now });
                TripStopCompletion.AddEvent(state, refreshId, "CALCULO", "Posição observada e percurso restante calculado sob demanda.", now);
                return Project(state, true);
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (TripNotFoundException) { throw; }
        catch (Exception exception)
        {
            ct.ThrowIfCancellationRequested();
            // The stale-data fallback hides the cause from the caller, so record it for diagnosis.
            _logger.LogWarning(exception, "Monitoring refresh failed for trip {TripId}; returning the previous snapshot.", tripId);
            if (previous.Snapshot == null) throw new TrackingUnavailableException();
            return Project(previous, false, true);
        }
    }

    private void ValidateFix(TrackingFix fix, TripState state, DateTimeOffset now)
    {
        var snapshot = state.Snapshot;
        if (fix.DeviceId != state.Vehicle.TraccarDeviceId || string.IsNullOrWhiteSpace(fix.ObservationId) || fix.ObservationId.Length > 128
            || !double.IsFinite(fix.Latitude) || !double.IsFinite(fix.Longitude) || Math.Abs(fix.Latitude) > 90 || Math.Abs(fix.Longitude) > 180
            || fix.FixTime > now || now - fix.FixTime > TimeSpan.FromMinutes(_settings.MaxGpsAgeMinutes)
            || (snapshot?.LastSuccessfulCalculationAt != null && fix.FixTime <= snapshot.LastPingAt)
            || (snapshot?.LastDeviceId == fix.DeviceId && snapshot.LastObservationId == fix.ObservationId))
            throw new TrackingUnavailableException();
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim().ToUpperInvariant();
    private static void Invalid(string field) => throw new ValidationException(new[] { new ValidationError(field, "Invalid monitoring filter or page.") });
}
