using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class PartnerNetworkService : IPartnerNetworkService
{
    private readonly IPartnerConnectionRepository _connections;
    private readonly IVehicleRepository _vehicles;
    private readonly ITripRepository _trips;
    private readonly IAuctionRepository _auctions;
    private readonly INotificationPublisher _notifications;

    public PartnerNetworkService(
        IPartnerConnectionRepository connections,
        IVehicleRepository vehicles,
        ITripRepository trips,
        IAuctionRepository auctions,
        INotificationPublisher? notifications = null)
    {
        _connections = connections;
        _vehicles = vehicles;
        _trips = trips;
        _auctions = auctions;
        _notifications = notifications ?? NullNotificationPublisher.Instance;
    }

    public async Task<PartnerNetworkDto> GetNetworkAsync(Guid contractorId, CancellationToken cancellationToken = default)
    {
        var connections = (await _connections.GetByContractorAsync(contractorId, cancellationToken)).ToList();

        var totalActive = connections.Count(c => c.Status == PartnershipStatus.Active);
        var totalPending = connections.Count(c => c.Status == PartnershipStatus.Pending);

        var carrierIds = connections.Select(c => c.CarrierId).Distinct().ToList();
        var freeVehicleCounts = await _vehicles.CountFreeByCarrierIdsAsync(carrierIds, cancellationToken);
        var activeTripCounts = await _trips.CountActiveByCarrierForContractorAsync(contractorId, carrierIds, cancellationToken);

        var partners = connections
            .Select(connection => MontarDto(connection, freeVehicleCounts, activeTripCounts))
            .ToList();

        return new PartnerNetworkDto(totalActive, totalPending, partners);
    }

    public async Task<CarrierNetworkDto> GetCarrierNetworkAsync(Guid carrierId, CancellationToken cancellationToken = default)
    {
        var connections = (await _connections.GetByCarrierAsync(carrierId, cancellationToken)).ToList();

        var totalActive = connections.Count(c => c.Status == PartnershipStatus.Active);
        var totalPending = connections.Count(c => c.Status == PartnershipStatus.Pending);

        var contractorIds = connections.Select(c => c.ContractorId).Distinct().ToList();
        var openOffers = await _auctions.CountOpenByContractorsAsync(contractorIds, DateTimeOffset.UtcNow, cancellationToken);
        var activeLanes = await _trips.CountActiveLanesByContractorForCarrierAsync(carrierId, contractorIds, cancellationToken);

        var partners = connections
            .Select(connection => MontarContratanteDto(connection, openOffers, activeLanes))
            .ToList();

        return new CarrierNetworkDto(totalActive, totalPending, partners);
    }

    public async Task RemovePartnershipAsync(Guid companyId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _connections.GetTrackedByIdAsync(connectionId, cancellationToken);

        // Scope to the requesting company: only a party of the partnership can end it.
        if (connection is null ||
            (connection.ContractorId != companyId && connection.CarrierId != companyId))
        {
            throw new PartnershipNotFoundException(connectionId);
        }

        // Already ended: idempotent no-op.
        if (connection.Status == PartnershipStatus.Rejected)
        {
            return;
        }

        // Soft end: keep the record so it stays visible as an inactive partnership.
        connection.Status = PartnershipStatus.Rejected;
        await _connections.UpdateAsync(connection, cancellationToken);

        // The other side learns who ended it.
        var endedByContractor = connection.ContractorId == companyId;
        var endedBy = endedByContractor
            ? connection.Contractor is { } contractor ? NotificationCatalog.CompanyName(contractor.CompanyName, contractor.TradeName) : "O embarcador"
            : connection.Carrier is { } carrier ? NotificationCatalog.CompanyName(carrier.CompanyName, carrier.TradeName) : "A transportadora";
        await _notifications.PublishAsync([NotificationCatalog.PartnershipEnded(
            endedByContractor ? connection.CarrierId : connection.ContractorId, endedBy)]);
    }

    private static CarrierNetworkPartnerDto MontarContratanteDto(
        PartnerConnection connection,
        Dictionary<Guid, int> openOffers,
        Dictionary<Guid, int> activeLanes)
    {
        var contractor = connection.Contractor;

        var contractorDto = new ContractorSummaryDto(
            contractor?.Id ?? connection.ContractorId,
            contractor?.TradeName ?? contractor?.CompanyName ?? "(contratante não encontrado)",
            contractor is not null ? CnpjFormatter.Format(contractor.Cnpj) : string.Empty);

        return new CarrierNetworkPartnerDto(
            connection.Id,
            MapStatus(connection.Status),
            contractorDto,
            connection.UpdatedAt,
            // An ended or pending partnership cannot see the contractor's offers.
            OpenOffers: connection.Status == PartnershipStatus.Active ? openOffers.GetValueOrDefault(connection.ContractorId, 0) : 0,
            ActiveLanes: activeLanes.GetValueOrDefault(connection.ContractorId, 0));
    }

    private static PartnerDto MontarDto(
        PartnerConnection connection,
        Dictionary<Guid, int> freeVehicleCounts,
        Dictionary<Guid, int> activeTripCounts)
    {
        var carrier = connection.Carrier;

        var carrierDto = new CarrierSummaryDto(
            carrier?.Id ?? connection.CarrierId,
            carrier?.TradeName ?? carrier?.CompanyName ?? "(transportadora não encontrada)",
            carrier is not null ? CnpjFormatter.Format(carrier.Cnpj) : string.Empty,
            carrier?.AverageRating,
            carrier?.HasActiveInsurancePolicy ?? false);

        var freeVehicles = freeVehicleCounts.GetValueOrDefault(connection.CarrierId, 0);

        var activeTrips = activeTripCounts.GetValueOrDefault(connection.CarrierId, 0);

        var metrics = new OperationalMetricsDto(freeVehicles, activeTrips, connection.UpdatedAt);

        return new PartnerDto(
            connection.Id,
            MapStatus(connection.Status),
            carrierDto,
            metrics);
    }

    /// <summary>
    /// O enum PartnershipStatus é em inglês (convenção do resto do código), mas a spec
    /// do front exige os valores literais em português. Mapeamento explícito em vez de
    /// ToString() pra não depender por acidente do nome do enum.
    /// </summary>
    private static string MapStatus(PartnershipStatus status) => status switch
    {
        PartnershipStatus.Active => "ATIVA",
        PartnershipStatus.Pending => "PENDENTE",
        PartnershipStatus.Rejected => "RECUSADA",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status de parceria não mapeado.")
    };
}