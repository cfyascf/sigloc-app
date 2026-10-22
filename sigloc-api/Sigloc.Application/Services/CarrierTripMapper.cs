using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>Enriches the shared monitoring detail with what a carrier needs to operate the trip.</summary>
public static class CarrierTripMapper
{
    public static CarrierTripDetailDto Map(TripState state, TripDetailDto detail)
    {
        var shipper = state.Shipper;
        var cargo = ConsolidatedCargoProfile.From(state.Segments.SelectMany(s => s.Items));
        return new CarrierTripDetailDto(
            detail,
            shipper == null ? null : new TripShipperContactDto(shipper.CompanyName, shipper.TradeName,
                shipper.LogisticsContactName, shipper.LogisticsContactPhone, shipper.LogisticsContactEmail),
            new TripCargoSpecsDto(
                cargo.Environment.ToWire(), cargo.Environment != TransportEnvironment.Dry, cargo.TempMin, cargo.TempMax,
                cargo.Palletized, cargo.Packagings, cargo.Requirement.RequiresMopp, cargo.Requirement.RequiresCargoFixing,
                cargo.Requirement.BaseBodyworkType, Positive(state.Route.TotalWeightKg), Positive(state.Route.TotalVolumeM3),
                cargo.Products.Select(p => new TripCargoProductDto(p.Name, p.Quantity)).ToList()),
            CurrentAction(state),
            state.Occurrences.OrderByDescending(o => o.ReportedAt).ThenBy(o => o.Id)
                .Select(o => new TripOccurrenceDto(o.Id, o.Type.ToWire(), o.Type.ToLabel(), o.Severity, o.Note, o.ReportedAt, o.BlockingStopId, o.ResolvedAt))
                .ToList(),
            state.Proofs.OrderByDescending(p => p.UploadedAt).ThenBy(p => p.Id)
                .Select(p => new TripProofOfDeliveryDto(p.Id, p.TripStopId, p.FileName, p.ContentType, p.SizeBytes, p.UploadedAt))
                .ToList());
    }

    private static TripCurrentActionDto? CurrentAction(TripState state)
    {
        if (TripMonitoringCalculator.IsTerminal(state.Trip)) return null;
        var next = TripStopCompletion.FirstPending(state.Trip);
        if (next == null) return null;
        var pending = next.Actions.Where(a => !a.IsCompleted).ToList();
        var pickup = pending.Any(a => a.Kind == StopActionKind.Pickup);
        var delivery = pending.Any(a => a.Kind == StopActionKind.Delivery);
        var kind = pickup && delivery ? "COLETA_E_ENTREGA" : delivery ? "ENTREGA" : "COLETA";
        return new TripCurrentActionDto(next.Id, kind, delivery, TripDetailMapper.NextDeadline(next));
    }

    private static double? Positive(double value) => double.IsFinite(value) && value > 0 ? value : null;
}
