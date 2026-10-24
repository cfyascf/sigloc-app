using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using static Sigloc.Tests.TripMonitoringServiceTests;

namespace Sigloc.Tests;

public class CarrierTripServiceTests
{
    private static readonly byte[] Pdf = [.. "%PDF-1.7\n"u8.ToArray(), 1, 2, 3];

    [Fact]
    public async Task Pickup_confirmation_starts_trip_and_points_to_delivery()
    {
        var c = new CarrierHarness();
        var result = await c.Complete(c.Stops[0].Id);

        var state = c.H.Store.State!;
        Assert.Equal("DRIVER_CONFIRMED", state.Trip.Stops[0].CompletionSource);
        Assert.Equal("DRIVER_CONFIRMED", state.Trip.Stops[0].Actions[0].CompletionSource);
        Assert.Equal(TripStatus.InTransit, state.Trip.Status);
        Assert.Equal(Now, state.Trip.StartedAt);
        Assert.Equal(SegmentStatus.InTransit, state.Segments[0].Status);
        Assert.Single(state.Events, e => e.Kind == "PARADA_CONFIRMADA");
        Assert.Equal("EM_TRANSITO", result.Trip.Status);
        Assert.Equal(c.Stops[1].Id, result.CurrentAction!.StopId);
        Assert.Equal("ENTREGA", result.CurrentAction.Kind);
        Assert.True(result.CurrentAction.RequiresProof);
        Assert.Empty(c.Files.Stored);
        var notice = Assert.Single(c.H.Notifications.Sent);
        Assert.Equal((c.H.CompanyId, NotificationCatalog.PickupConfirmedType), (notice.RecipientCompanyId, notice.Type));
        Assert.Equal($"/active-route-tracking/{c.H.State.Trip.Id}", notice.Link);
    }

    [Fact]
    public async Task Delivery_without_proof_is_rejected_without_changes()
    {
        var c = new CarrierHarness(); await c.Complete(c.Stops[0].Id);

        var error = await Assert.ThrowsAsync<ValidationException>(() => c.Complete(c.Stops[1].Id));

        Assert.Equal("comprovante", Assert.Single(error.Errors).Field);
        Assert.False(c.H.Store.State!.Trip.Stops[1].IsCompleted);
    }

    [Fact]
    public async Task Delivery_with_proof_stores_file_and_finishes_trip()
    {
        var c = new CarrierHarness(); await c.Complete(c.Stops[0].Id);

        var result = await c.Complete(c.Stops[1].Id, Pdf, "Recebido por João");

        var state = c.H.Store.State!;
        var proof = Assert.Single(state.Proofs);
        Assert.Equal(c.Stops[1].Id, proof.TripStopId);
        Assert.Equal("application/pdf", proof.ContentType);
        Assert.Equal(c.UserId, proof.UploadedByUserId);
        Assert.Equal(Pdf, c.Files.Stored[proof.StorageKey]);
        Assert.StartsWith($"trips/{c.H.State.Trip.Id:N}/", proof.StorageKey);
        Assert.Equal("POD_CONFIRMED", state.Trip.Stops[1].CompletionSource);
        Assert.Equal(TripStatus.Delivered, state.Trip.Status);
        Assert.Equal(RouteStatus.Completed, state.Route.Status);
        Assert.Equal(SegmentStatus.Completed, state.Segments[0].Status);
        Assert.Contains(state.Events, e => e.Kind == "PARADA_CONFIRMADA" && e.Description.Contains("Recebido por João"));
        Assert.Equal("ENTREGUE", result.Trip.Status);
        Assert.Null(result.CurrentAction);
        Assert.Single(result.Proofs);
        // The last delivery reports the whole trip as delivered.
        Assert.Equal(new[] { NotificationCatalog.PickupConfirmedType, NotificationCatalog.TripDeliveredType }, c.H.Notifications.Sent.Select(n => n.Type));
    }

    [Fact]
    public async Task Only_first_pending_stop_is_actionable_and_orphan_file_is_deleted()
    {
        var c = new CarrierHarness();

        await Assert.ThrowsAsync<TripStopNotActionableException>(() => c.Complete(c.Stops[1].Id, Pdf));

        Assert.Empty(c.Files.Stored);
        Assert.Single(c.Files.Deleted);
        Assert.Empty(c.H.Store.State!.Proofs);
    }

    [Fact]
    public async Task Unknown_stop_is_not_found()
    {
        var c = new CarrierHarness();
        await Assert.ThrowsAsync<TripStopNotFoundException>(() => c.Complete(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(TripStatus.Delivered)] [InlineData(TripStatus.Cancelled)]
    public async Task Terminal_trip_rejects_actions(TripStatus status)
    {
        var c = new CarrierHarness(); c.H.State.Trip.Status = status;
        await Assert.ThrowsAsync<TripNotActiveException>(() => c.Complete(c.Stops[0].Id));
        await Assert.ThrowsAsync<TripNotActiveException>(() => c.Report("AVARIA_MECANICA"));
    }

    [Fact]
    public async Task Other_carrier_cannot_see_or_act_on_trip()
    {
        var c = new CarrierHarness();
        await Assert.ThrowsAsync<TripNotFoundException>(() => c.Service.GetDetailAsync(Guid.NewGuid(), c.H.State.Trip.Id));
        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            c.Service.CompleteStopAsync(Guid.NewGuid(), c.UserId, c.H.State.Trip.Id, c.Stops[0].Id, null, null));
        // The shipper scope is unaffected by the carrier id.
        Assert.NotNull(await c.H.Service.GetDetailAsync(c.H.CompanyId, c.H.State.Trip.Id));
    }

    [Fact]
    public async Task Manual_completion_invalidates_eta_so_next_ping_recalculates()
    {
        var c = new CarrierHarness(); c.H.Cached(1);

        await c.Complete(c.Stops[0].Id);

        var snapshot = c.H.Store.State!.Snapshot!;
        Assert.Null(snapshot.Risk);
        Assert.Null(snapshot.LastCalculatedEta);
        Assert.Equal(c.Stops[1].Id, snapshot.NextStopId);
        Assert.Equal(c.Stops[1].Actions[0].Deadline, snapshot.NextStopDeadline);
        Assert.Contains(c.H.Store.State.Events, e => e.Kind == "SLA");

        c.H.Tracking.Fix = c.H.Tracking.Fix with { FixTime = Now };
        var refreshed = await c.Service.GetDetailAsync(c.CarrierId, c.H.State.Trip.Id, refresh: true);
        Assert.Equal(1, c.H.Tracking.Calls);
        Assert.True(refreshed.Trip.IsCacheRenewed);
    }

    [Theory]
    [InlineData("AVARIA_MECANICA", "CRITIC", "ATRASADO")]
    [InlineData("risco_sla", "CRITIC", "ATRASADO")]
    [InlineData("TRANSITO_INTENSO", "WARNING", "AGUARDANDO_COLETA")]
    [InlineData("FALTA_DOCUMENTACAO", "WARNING", "AGUARDANDO_COLETA")]
    public async Task Occurrence_raises_effective_risk_and_blocks_on_next_stop(string type, string severity, string status)
    {
        var c = new CarrierHarness();

        var result = await c.Report(type, "  Pneu furado na BR-277  ");

        var occurrence = Assert.Single(c.H.Store.State!.Occurrences);
        Assert.Equal(severity, occurrence.Severity);
        Assert.Equal(c.Stops[0].Id, occurrence.BlockingStopId);
        Assert.Equal("Pneu furado na BR-277", occurrence.Note);
        Assert.Equal(c.UserId, occurrence.ReportedByUserId);
        Assert.Equal(severity, result.Trip.Risk);
        Assert.Equal(status, result.Trip.Status);
        Assert.True(Assert.Single(result.Occurrences).IsOpen);
        Assert.Single(c.H.Store.State.Events, e => e.Kind == "OCORRENCIA");
        Assert.Single(c.H.Store.State.Events, e => e.Kind == "SLA");
        var notice = Assert.Single(c.H.Notifications.Sent);
        Assert.Equal((c.H.CompanyId, NotificationCatalog.OccurrenceType), (notice.RecipientCompanyId, notice.Type));
        Assert.Equal(severity == "CRITIC", notice.Message.Contains("crítica"));
    }

    [Fact]
    public async Task Completing_blocking_stop_closes_occurrence()
    {
        var c = new CarrierHarness(); await c.Report("AVARIA_MECANICA");

        var result = await c.Complete(c.Stops[0].Id);

        Assert.False(Assert.Single(result.Occurrences).IsOpen);
        Assert.Equal("DRIVER_CONFIRMED", c.H.Store.State!.Occurrences[0].ResolutionSource);
        Assert.Equal("NAO_MONITORADO", result.Trip.Risk);
        Assert.Contains(c.H.Store.State.Events, e => e.Kind == "OCORRENCIA_ENCERRADA");
    }

    [Theory]
    [InlineData(null, null, "tipo")]
    [InlineData("ATENCAO", null, "tipo")]
    [InlineData("AVARIA_MECANICA", 501, "observacao")]
    public async Task Invalid_occurrence_is_rejected(string? type, int? noteLength, string field)
    {
        var c = new CarrierHarness();
        var error = await Assert.ThrowsAsync<ValidationException>(() => c.Report(type, noteLength == null ? null : new string('x', noteLength.Value)));
        Assert.Equal(field, Assert.Single(error.Errors).Field);
        Assert.Equal(0, c.H.Store.RefreshCalls);
    }

    [Fact]
    public async Task Detail_exposes_cargo_specs_shipper_contact_and_current_action()
    {
        var c = new CarrierHarness();
        var frozen = Product("Sorvete", TransportEnvironment.Frozen, -25, -15, PackagingType.Palletized);
        var chilled = Product("Iogurte", TransportEnvironment.Chilled, -20, -10, PackagingType.MasterCartons);
        c.H.State.Segments[0].Items = [Item(frozen, 10), Item(chilled, 4), Item(frozen, 2)];
        c.H.State.Route.TotalWeightKg = 1200;
        c.H.Store.State = c.H.State with
        {
            Shipper = new Contractor { Id = c.H.CompanyId, Cnpj = "1", CompanyName = "Embarcador SA", TradeName = "Embarcador",
                LogisticsContactName = "Mesa", LogisticsContactPhone = "(41) 3333-0000", LogisticsContactEmail = "mesa@emb.com.br" }
        };

        var result = await c.Service.GetDetailAsync(c.CarrierId, c.H.State.Trip.Id);

        Assert.Equal("Congelado", result.Cargo.Environment);
        Assert.True(result.Cargo.Refrigerated);
        Assert.Equal(-20, result.Cargo.TempMin);
        Assert.Equal(-15, result.Cargo.TempMax);
        Assert.True(result.Cargo.Palletized);
        Assert.Equal(["Paletizado", "Caixas Master"], result.Cargo.Packagings);
        Assert.Equal(1200, result.Cargo.TotalWeightKg);
        Assert.Equal(30, result.Cargo.TotalVolumeM3);
        Assert.Contains(result.Cargo.Products, p => p.Name == "Sorvete" && p.Quantity == 12);
        Assert.Equal("Mesa", result.Shipper!.ContactName);
        Assert.Equal("(41) 3333-0000", result.Shipper.Phone);
        Assert.Equal(c.Stops[0].Id, result.CurrentAction!.StopId);
        Assert.Equal("COLETA", result.CurrentAction.Kind);
        Assert.False(result.CurrentAction.RequiresProof);
        Assert.Equal(0, c.H.Tracking.Calls);
    }

    [Fact]
    public async Task Proof_can_be_downloaded_only_within_scope()
    {
        var c = new CarrierHarness(); await c.Complete(c.Stops[0].Id);
        await c.Complete(c.Stops[1].Id, Pdf);
        var proof = c.H.Store.State!.Proofs[0];

        var file = await c.Service.OpenProofAsync(c.CarrierId, c.H.State.Trip.Id, proof.Id);

        Assert.Equal("application/pdf", file.ContentType);
        using var copy = new MemoryStream(); await file.Content.CopyToAsync(copy);
        Assert.Equal(Pdf, copy.ToArray());
        await Assert.ThrowsAsync<ProofOfDeliveryNotFoundException>(() => c.Service.OpenProofAsync(c.CarrierId, c.H.State.Trip.Id, Guid.NewGuid()));
        await Assert.ThrowsAsync<TripNotFoundException>(() => c.Service.OpenProofAsync(Guid.NewGuid(), c.H.State.Trip.Id, proof.Id));
    }

    [Fact]
    public async Task Carrier_list_is_scoped_to_carrier()
    {
        var c = new CarrierHarness();
        await c.Service.SearchAsync(c.CarrierId, new TripQueryDto());
        Assert.Equal(TripScope.ForCarrier(c.CarrierId), c.H.Store.LastScope);
    }

    private static Product Product(string name, TransportEnvironment environment, double min, double max, PackagingType packaging) => new()
    {
        Id = Guid.NewGuid(), Sku = name, Name = name, TransportEnvironment = environment, TempMin = min, TempMax = max, PackagingType = packaging
    };

    private static ProductRouteSegment Item(Product product, int quantity) =>
        new() { Id = Guid.NewGuid(), ProductId = product.Id, Product = product, Quantity = quantity };

    internal sealed class CarrierHarness
    {
        public Harness H { get; } = new();
        public Guid CarrierId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public FakeFileStorage Files { get; } = new();
        public CarrierTripService Service { get; }
        public List<TripStop> Stops => H.State.Trip.Stops.OrderBy(s => s.Sequence).ToList();

        public CarrierHarness()
        {
            H.State.Trip.CarrierId = CarrierId;
            Service = new CarrierTripService(H.Service, H.Store, Files, H.Clock, notifications: H.Notifications);
        }

        public Task<CarrierTripDetailDto> Complete(Guid stopId, byte[]? file = null, string? note = null) =>
            Service.CompleteStopAsync(CarrierId, UserId, H.State.Trip.Id, stopId,
                file == null ? null : new UploadedFileDto(new MemoryStream(file), "canhoto.pdf", file.Length), note);

        public Task<CarrierTripDetailDto> Report(string? type, string? note = null) =>
            Service.ReportOccurrenceAsync(CarrierId, UserId, H.State.Trip.Id, new ReportTripOccurrenceRequestDto(type, note));
    }

    internal sealed class FakeFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Stored { get; } = new();
        public List<string> Deleted { get; } = new();

        public async Task<string> SaveAsync(string prefix, Stream content, string extension, CancellationToken ct = default)
        {
            using var copy = new MemoryStream(); await content.CopyToAsync(copy, ct);
            var key = $"{prefix}/{Guid.NewGuid():N}{extension}";
            Stored[key] = copy.ToArray();
            return key;
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default) =>
            Task.FromResult<Stream?>(Stored.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            Stored.Remove(key); Deleted.Add(key);
            return Task.CompletedTask;
        }
    }
}
