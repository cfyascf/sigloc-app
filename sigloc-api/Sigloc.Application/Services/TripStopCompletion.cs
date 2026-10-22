using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>
/// Itinerary state transitions shared by GPS geofence inference and manual driver confirmation, so
/// both paths move stops, segments, the trip and its route through exactly the same lifecycle.
/// </summary>
public static class TripStopCompletion
{
    /// <summary>Builds the stop itinerary lazily for trips awarded before itineraries were persisted.</summary>
    public static void EnsureItinerary(TripState state)
    {
        if (state.Trip.Stops.Count == 0)
            state.Trip.Stops = TripItineraryBuilder.Build(state.Trip.Id, state.Segments);
    }

    public static List<TripStop> Pending(Trip trip) =>
        trip.Stops.Where(s => !s.IsCompleted).OrderBy(s => s.Sequence).ToList();

    public static TripStop? FirstPending(Trip trip) => Pending(trip).FirstOrDefault();

    /// <summary>
    /// Completes <paramref name="stop"/> and its pending actions. <paramref name="sourceFor"/> chooses the
    /// completion source per action kind; the stop takes the delivery source when it delivered anything.
    /// Occurrences blocked on this stop are closed with a single summary event.
    /// </summary>
    public static void Complete(TripState state, TripStop stop, DateTimeOffset at, Func<StopActionKind, string> sourceFor,
        Guid operationId, DateTimeOffset now)
    {
        var pendingActions = stop.Actions.Where(a => !a.IsCompleted).ToList();
        foreach (var action in pendingActions)
        {
            action.IsCompleted = true; action.CompletedAt = at; action.CompletionSource = sourceFor(action.Kind);
            var segment = state.Segments.Single(s => s.Id == action.SegmentId);
            segment.Status = action.Kind == StopActionKind.Delivery ? SegmentStatus.Completed : SegmentStatus.InTransit;
        }
        stop.IsCompleted = true; stop.CompletedAt = at;
        stop.CompletionSource = sourceFor(pendingActions.Any(a => a.Kind == StopActionKind.Delivery) ? StopActionKind.Delivery : StopActionKind.Pickup);
        if (state.Trip.Status == TripStatus.AwaitingPickup)
        {
            state.Trip.Status = TripStatus.InTransit; state.Trip.StartedAt ??= at;
            state.Route.Status = RouteStatus.InTransit;
        }
        var resolved = state.Occurrences.Where(o => o.ResolvedAt == null && o.BlockingStopId == stop.Id).ToList();
        foreach (var occurrence in resolved)
        {
            occurrence.ResolvedAt = at; occurrence.ResolutionSource = stop.CompletionSource;
        }
        if (resolved.Count > 0)
            AddEvent(state, operationId, "OCORRENCIA_ENCERRADA",
                $"{resolved.Count} ocorrência(s) encerrada(s) com a conclusão da parada {stop.Sequence}.", now);
        if (FirstPending(state.Trip) == null) Finish(state, at);
    }

    public static void Finish(TripState state, DateTimeOffset at)
    {
        state.Trip.Status = TripStatus.Delivered; state.Trip.FinishedAt = at;
        state.Route.Status = RouteStatus.Completed;
    }

    public static void AddEvent(TripState state, Guid operationId, string kind, string description, DateTimeOffset now) =>
        state.Events.Add(new TripMonitoringEvent { Id = Guid.NewGuid(), TripId = state.Trip.Id, RefreshId = operationId, Kind = kind, Description = description, OccurredAt = now });
}
