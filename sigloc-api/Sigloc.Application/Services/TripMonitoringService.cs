using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

public sealed class TripMonitoringService(
    ITripMonitoringStore store, ITripTrackingProvider tracking, ITripRoutingProvider routing,
    TimeProvider clock, TripMonitoringOptions options) : ITripMonitoringService
{
    private readonly TripMonitoringOptions _settings = options;

    public Task<PagedTripsDto> SearchAsync(Guid contractorId, TripQueryDto query, CancellationToken ct = default)
    {
        var status = Normalize(query.Status);
        var risk = Normalize(query.Risk);
        if (status != null && status is not ("AGUARDANDO_COLETA" or "EM_TRANSITO" or "ATRASADO")) Invalid("status");
        if (risk != null && risk is not ("NORMAL" or "CRITIC" or "NAO_MONITORADO")) Invalid("risk");
        var size = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var page = Math.Max(query.Page, 1);
        if ((long)(page - 1) * size > int.MaxValue) Invalid("page");
        return store.SearchAsync(contractorId, query with { Page = page, PageSize = size, Search = query.Search?.Trim(), Status = status, Risk = risk }, ct);
    }

    public async Task<TripDetailDto> GetDetailAsync(Guid contractorId, Guid tripId, CancellationToken ct = default)
    {
        var previous = await store.ReadAsync(contractorId, tripId, ct) ?? throw new TripNotFoundException(tripId);
        if (TripMonitoringCalculator.IsTerminal(previous.Trip) || TripMonitoringCalculator.IsFresh(previous.Snapshot, clock.GetUtcNow()))
            return Map(previous, false);

        // Memoize provider work across EF retries; each attempt receives a new tracked graph.
        TrackingFix? fix = null;
        var estimates = new Dictionary<string, TripRouteEstimate>();
        var refreshId = Guid.NewGuid();
        try
        {
            return await store.RefreshAsync(contractorId, tripId, async (state, token) =>
            {
                var now = clock.GetUtcNow();
                if (TripMonitoringCalculator.IsTerminal(state.Trip) || TripMonitoringCalculator.IsFresh(state.Snapshot, now))
                    return Map(state, false);
                if (state.Trip.Stops.Count == 0)
                    state.Trip.Stops = TripItineraryBuilder.Build(state.Trip.Id, state.Segments);
                if (state.Trip.Stops.Count == 0 || state.Vehicle.TraccarDeviceId is not > 0)
                    throw new TrackingUnavailableException();
                fix ??= await tracking.GetLatestAsync(state.Vehicle.TraccarDeviceId.Value, now, token);
                ValidateFix(fix, state, now);
                var pending = state.Trip.Stops.Where(s => !s.IsCompleted).OrderBy(s => s.Sequence).ToList();
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
                    reached.IsCompleted = true; reached.CompletedAt = fix.FixTime; reached.CompletionSource = "GEOFENCE_INFERRED";
                    foreach (var action in reached.Actions.Where(a => !a.IsCompleted))
                    {
                        action.IsCompleted = true; action.CompletedAt = fix.FixTime; action.CompletionSource = "GEOFENCE_INFERRED";
                        var segment = state.Segments.Single(s => s.Id == action.SegmentId);
                        segment.Status = action.Kind == StopActionKind.Delivery ? SegmentStatus.Completed : SegmentStatus.InTransit;
                    }
                    if (state.Trip.Status == TripStatus.AwaitingPickup)
                    {
                        state.Trip.Status = TripStatus.InTransit; state.Trip.StartedAt ??= fix.FixTime;
                        state.Route.Status = RouteStatus.InTransit;
                    }
                    AddEvent(state, refreshId, "PARADA_INFERIDA", $"Entrada na cerca da parada {reached.Sequence}; coleta/entrega inferida, não verificada.", now);
                }
                if (finished)
                {
                    state.Trip.Status = TripStatus.Delivered; state.Trip.FinishedAt = fix.FixTime;
                    state.Route.Status = RouteStatus.Completed;
                }
                var snapshot = state.Snapshot ??= new TripMonitoring { Id = Guid.NewGuid(), TripId = tripId };
                if (snapshot.Risk != risk) AddEvent(state, refreshId, "SLA", risk == "CRITIC" ? "ETA calculada excede o prazo da próxima parada." : "SLA recalculado sem atraso previsto.", now);
                snapshot.LastSuccessfulCalculationAt = now;
                snapshot.LastPingAt = fix.FixTime; snapshot.LastObservationId = fix.ObservationId; snapshot.LastDeviceId = fix.DeviceId;
                snapshot.Latitude = fix.Latitude; snapshot.Longitude = fix.Longitude;
                snapshot.LastProgressPercentage = progress; snapshot.TraveledDistanceKm = traveled;
                snapshot.RemainingDistanceKm = estimate.RemainingDistanceKm; snapshot.LastCalculatedEta = eta;
                snapshot.NextStopId = next?.Id; snapshot.NextStopDeadline = deadline; snapshot.Risk = risk;
                state.NewTelemetry.Add(new TripTelemetry { Id = refreshId, TripId = tripId, ObservationId = fix.ObservationId,
                    DeviceId = fix.DeviceId, Latitude = fix.Latitude, Longitude = fix.Longitude, FixTime = fix.FixTime, CalculatedAt = now });
                AddEvent(state, refreshId, "CALCULO", "Posição observada e percurso restante calculado sob demanda.", now);
                return Map(state, true);
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (TripNotFoundException) { throw; }
        catch (Exception)
        {
            ct.ThrowIfCancellationRequested();
            if (previous.Snapshot == null) throw new TrackingUnavailableException();
            return Map(previous, false, true);
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

    private static void AddEvent(TripState state, Guid refreshId, string kind, string description, DateTimeOffset now) =>
        state.Events.Add(new TripMonitoringEvent { Id = Guid.NewGuid(), TripId = state.Trip.Id, RefreshId = refreshId, Kind = kind, Description = description, OccurredAt = now });

    private static TripDetailDto Map(TripState state, bool renewed, bool stale = false)
    {
        var m = state.Snapshot;
        var next = state.Trip.Stops.Where(s => !s.IsCompleted).OrderBy(s => s.Sequence).FirstOrDefault();
        return new TripDetailDto(state.Trip.Id, state.Route.Id, "TRP-" + state.Trip.Id.ToString("N")[..6].ToUpperInvariant(),
            TripMonitoringCalculator.Status(state.Trip, m), m?.Risk ?? "NAO_MONITORADO",
            state.Carrier.TradeName ?? state.Carrier.CompanyName, state.Vehicle.Plate, state.Vehicle.Driver, state.Vehicle.DriverPhone,
            m?.LastProgressPercentage, double.IsFinite(state.Route.TotalDistanceKm) && state.Route.TotalDistanceKm > 0 ? state.Route.TotalDistanceKm : 0,
            m?.TraveledDistanceKm, m?.RemainingDistanceKm,
            TripMonitoringCalculator.Utilization(state.Route.TotalVolumeM3, state.Vehicle.CapacityVolume), m?.LastCalculatedEta,
            m?.NextStopDeadline, m?.Latitude, m?.Longitude, m?.LastPingAt, m?.LastSuccessfulCalculationAt, renewed, stale,
            stale ? "Não foi possível atualizar o monitoramento. Exibindo a última posição disponível." : null,
            state.Trip.Stops.OrderBy(s => s.Sequence).Select(s => new TripStopDto(s.Id, s.Sequence, s.City, s.State, s.Latitude, s.Longitude,
                s.IsCompleted ? "CONCLUIDA" : s == next ? "EM_TRANSITO" : "PENDENTE", s.CompletedAt, s.CompletionSource,
                s.Actions.OrderBy(a => a.Kind).ThenBy(a => a.Deadline).ThenBy(a => a.Id).Select(a => new TripActionDto(a.Id, a.SegmentId, a.ProductName,
                    a.Kind == StopActionKind.Pickup ? "COLETA" : "ENTREGA", a.Deadline, a.CompletedAt, a.CompletionSource)).ToList())).ToList(),
            state.Events.OrderByDescending(e => e.OccurredAt).ThenBy(e => e.Id).Take(50).Select(e => new TripEventDto(e.Id, e.Kind, e.Description, e.OccurredAt)).ToList());
    }
    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim().ToUpperInvariant();
    private static void Invalid(string field) => throw new ValidationException(new[] { new ValidationError(field, "Invalid monitoring filter or page.") });
}
