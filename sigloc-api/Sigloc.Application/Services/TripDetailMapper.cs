using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>Projects a loaded trip graph into the monitoring detail shared by shippers and carriers.</summary>
public static class TripDetailMapper
{
    /// <summary>Proof-of-delivery files of the trip, newest first.</summary>
    public static IReadOnlyList<TripProofOfDeliveryDto> Proofs(TripState state) =>
        state.Proofs.OrderByDescending(p => p.UploadedAt).ThenBy(p => p.Id)
            .Select(p => new TripProofOfDeliveryDto(p.Id, p.TripStopId, p.FileName, p.ContentType, p.SizeBytes, p.UploadedAt))
            .ToList();

    public static string Reference(Guid tripId) => "TRP-" + tripId.ToString("N")[..6].ToUpperInvariant();

    public static TripDetailDto Map(TripState state, bool renewed, bool stale = false)
    {
        var m = state.Snapshot;
        var next = TripStopCompletion.FirstPending(state.Trip);
        var risk = TripRiskEvaluator.Effective(m?.Risk, state.Occurrences);
        return new TripDetailDto(state.Trip.Id, state.Route.Id, Reference(state.Trip.Id),
            TripMonitoringCalculator.Status(state.Trip, risk), risk,
            state.Carrier.TradeName ?? state.Carrier.CompanyName, state.Vehicle.Plate, state.Vehicle.Driver, state.Vehicle.DriverPhone,
            m?.LastProgressPercentage, double.IsFinite(state.Route.TotalDistanceKm) && state.Route.TotalDistanceKm > 0 ? state.Route.TotalDistanceKm : 0,
            m?.TraveledDistanceKm, m?.RemainingDistanceKm,
            TripMonitoringCalculator.Utilization(state.Route.TotalVolumeM3, state.Vehicle.CapacityVolume), m?.LastCalculatedEta,
            NextDeadline(next), m?.Latitude, m?.Longitude, m?.LastPingAt, m?.LastSuccessfulCalculationAt, renewed, stale,
            stale ? "Não foi possível atualizar o monitoramento. Exibindo a última posição disponível." : null,
            state.Trip.Stops.OrderBy(s => s.Sequence).Select(s => new TripStopDto(s.Id, s.Sequence, s.City, s.State, s.Latitude, s.Longitude,
                s.IsCompleted ? "CONCLUIDA" : s == next ? "EM_TRANSITO" : "PENDENTE", s.CompletedAt, s.CompletionSource,
                s.Actions.OrderBy(a => a.Kind).ThenBy(a => a.Deadline).ThenBy(a => a.Id).Select(a => new TripActionDto(a.Id, a.SegmentId, a.ProductName,
                    a.Kind == StopActionKind.Pickup ? "COLETA" : "ENTREGA", a.Deadline, a.CompletedAt, a.CompletionSource)).ToList())).ToList(),
            state.Events.OrderByDescending(e => e.OccurredAt).ThenBy(e => e.Id).Take(50).Select(e => new TripEventDto(e.Id, e.Kind, e.Description, e.OccurredAt)).ToList())
        {
            HasTracker = state.Vehicle.TraccarDeviceId is > 0,
        };
    }

    /// <summary>Earliest open action deadline of the first pending stop.</summary>
    public static DateTimeOffset? NextDeadline(TripStop? next) =>
        next?.Actions.Where(a => !a.IsCompleted).Min(a => (DateTimeOffset?)a.Deadline);
}
