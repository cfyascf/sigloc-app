using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

public sealed class CarrierTripService(
    ITripMonitoringService monitoring, ITripMonitoringStore store, IFileStorage files, TimeProvider clock,
    ILogger<CarrierTripService>? logger = null, INotificationPublisher? notifications = null) : ICarrierTripService
{
    private const int MaxNoteLength = 500;
    private readonly ILogger _logger = logger ?? NullLogger<CarrierTripService>.Instance;
    private readonly INotificationPublisher _notifications = notifications ?? NullNotificationPublisher.Instance;

    public Task<PagedTripsDto> SearchAsync(Guid carrierId, TripQueryDto query, CancellationToken ct = default) =>
        monitoring.SearchAsync(TripScope.ForCarrier(carrierId), query, ct);

    public Task<CarrierTripDetailDto> GetDetailAsync(Guid carrierId, Guid tripId, bool refresh = false, CancellationToken ct = default) =>
        monitoring.GetDetailAsync(TripScope.ForCarrier(carrierId), tripId, refresh, CarrierTripMapper.Map, ct);

    public async Task<CarrierTripDetailDto> ReportOccurrenceAsync(Guid carrierId, Guid userId, Guid tripId,
        ReportTripOccurrenceRequestDto request, CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();
        if (!TripOccurrenceMappings.TryParseOccurrenceType(request.Type, out var type))
            errors.Add(new ValidationError("tipo", "Tipo de ocorrência inválido."));
        var note = NormalizeNote(request.Note, errors);
        if (errors.Count > 0) throw new ValidationException(errors, "Não foi possível registrar a ocorrência.");

        NotificationMessage? notice = null;
        var detail = await store.UpdateAsync(TripScope.ForCarrier(carrierId), tripId, (state, _) =>
        {
            EnsureActive(state);
            var now = clock.GetUtcNow();
            var operationId = Guid.NewGuid();
            var riskBefore = TripRiskEvaluator.Effective(state.Snapshot?.Risk, state.Occurrences);
            var severity = TripRiskEvaluator.SeverityOf(type);
            notice = NotificationCatalog.OccurrenceReported(state.Route.ContractorId, state.Trip.Id, type.ToLabel(), severity == TripRiskEvaluator.Critical);
            state.Occurrences.Insert(0, new TripOccurrence
            {
                Id = Guid.NewGuid(), TripId = state.Trip.Id, Type = type, Severity = severity,
                Note = note, ReportedByUserId = userId, ReportedAt = now,
                BlockingStopId = TripStopCompletion.FirstPending(state.Trip)?.Id
            });
            TripStopCompletion.AddEvent(state, operationId, "OCORRENCIA", Truncate(
                $"Ocorrência reportada pelo motorista: {type.ToLabel()}" + (note == null ? "." : $" — {note}")), now);
            TripMonitoringService.AddSlaEventIfChanged(state, riskBefore, operationId, now);
            return Task.FromResult(CarrierTripMapper.Map(state, TripDetailMapper.Map(state, false)));
        }, ct);
        if (notice != null) await _notifications.PublishAsync([notice]);
        return detail;
    }

    public async Task<CarrierTripDetailDto> CompleteStopAsync(Guid carrierId, Guid userId, Guid tripId, Guid stopId,
        UploadedFileDto? proof, string? note, CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();
        var normalizedNote = NormalizeNote(note, errors);
        if (errors.Count > 0) throw new ValidationException(errors, "Não foi possível concluir a parada.");
        var file = proof == null ? null : await ProofOfDeliveryFileInspector.InspectAsync(proof, ct);

        // Stored before the locked update: the update may be retried, the upload must not be.
        string? key = null;
        if (file != null)
        {
            using var content = new MemoryStream(file.Content, writable: false);
            key = await files.SaveAsync($"trips/{tripId:N}", content, file.Extension, ct);
        }

        NotificationMessage? notice = null;
        CarrierTripDetailDto detail;
        try
        {
            detail = await store.UpdateAsync(TripScope.ForCarrier(carrierId), tripId, (state, _) =>
            {
                EnsureActive(state);
                var stop = state.Trip.Stops.FirstOrDefault(s => s.Id == stopId) ?? throw new TripStopNotFoundException(stopId);
                if (stop != TripStopCompletion.FirstPending(state.Trip)) throw new TripStopNotActionableException(stopId);
                var delivers = stop.Actions.Any(a => !a.IsCompleted && a.Kind == StopActionKind.Delivery);
                if (delivers && file == null)
                    throw new ValidationException([new ValidationError("comprovante", "Anexe o comprovante de entrega (canhoto).")],
                        "Não foi possível concluir a parada.");

                var now = clock.GetUtcNow();
                var operationId = Guid.NewGuid();
                var riskBefore = TripRiskEvaluator.Effective(state.Snapshot?.Risk, state.Occurrences);
                TripStopCompletion.Complete(state, stop, now,
                    kind => kind == StopActionKind.Delivery ? "POD_CONFIRMED" : "DRIVER_CONFIRMED", operationId, now);
                if (file != null)
                    state.Proofs.Insert(0, new TripProofOfDelivery
                    {
                        Id = Guid.NewGuid(), TripId = state.Trip.Id, TripStopId = stop.Id, StorageKey = key!,
                        FileName = file.FileName, ContentType = file.ContentType, SizeBytes = file.Content.LongLength,
                        UploadedByUserId = userId, UploadedAt = now
                    });
                TripStopCompletion.AddEvent(state, operationId, "PARADA_CONFIRMADA", Truncate(
                    $"Parada {stop.Sequence} ({stop.City}) confirmada pelo motorista: {(delivers ? "entrega com comprovante" : "coleta")}"
                    + (normalizedNote == null ? "." : $" — {normalizedNote}")), now);
                InvalidateEta(state);
                TripMonitoringService.AddSlaEventIfChanged(state, riskBefore, operationId, now);
                notice = NotificationCatalog.StopConfirmed(state.Route.ContractorId, state.Trip.Id, stop.City, delivers,
                    state.Trip.Status == TripStatus.Delivered);
                return Task.FromResult(CarrierTripMapper.Map(state, TripDetailMapper.Map(state, false)));
            }, ct);
        }
        catch
        {
            if (key != null) await DeleteQuietlyAsync(key);
            throw;
        }
        if (notice != null) await _notifications.PublishAsync([notice]);
        return detail;
    }

    public Task<ProofOfDeliveryFileDto> OpenProofAsync(Guid carrierId, Guid tripId, Guid proofId, CancellationToken ct = default) =>
        TripProofService.OpenAsync(store, files, TripScope.ForCarrier(carrierId), tripId, proofId, ct);

    private static void EnsureActive(TripState state)
    {
        if (TripMonitoringCalculator.IsTerminal(state.Trip)) throw new TripNotActiveException(state.Trip.Id);
        TripStopCompletion.EnsureItinerary(state);
    }

    /// <summary>
    /// The stored ETA belonged to the leg that just ended. Point the snapshot at the new next stop and clear
    /// the ETA and its risk so the next GPS ping recalculates instead of serving the 15-minute cache.
    /// </summary>
    private static void InvalidateEta(TripState state)
    {
        if (state.Snapshot is not { } snapshot) return;
        var next = TripStopCompletion.FirstPending(state.Trip);
        snapshot.NextStopId = next?.Id;
        snapshot.NextStopDeadline = TripDetailMapper.NextDeadline(next);
        snapshot.LastCalculatedEta = null;
        snapshot.Risk = null;
    }

    private static string? NormalizeNote(string? note, List<ValidationError> errors)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed?.Length > MaxNoteLength) errors.Add(new ValidationError("observacao", "Máximo de 500 caracteres."));
        return trimmed;
    }

    private static string Truncate(string text) => text.Length <= MaxNoteLength ? text : text[..(MaxNoteLength - 1)] + "…";

    private async Task DeleteQuietlyAsync(string key)
    {
        try { await files.DeleteAsync(key); }
        catch (Exception exception) { _logger.LogWarning(exception, "Could not delete orphaned proof of delivery {StorageKey}.", key); }
    }
}
