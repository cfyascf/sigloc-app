using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class PartnerNetworkService : IPartnerNetworkService
{
    private readonly IPartnerConnectionRepository _connections;
    private readonly IVehicleRepository _vehicles;

    public PartnerNetworkService(IPartnerConnectionRepository connections, IVehicleRepository vehicles)
    {
        _connections = connections;
        _vehicles = vehicles;
    }

    public async Task<PartnerNetworkDto> GetNetworkAsync(Guid contractorId, CancellationToken cancellationToken = default)
    {
        var connections = (await _connections.GetByContractorAsync(contractorId, cancellationToken)).ToList();

        var totalActive = connections.Count(c => c.Status == PartnershipStatus.Active);
        var totalPending = connections.Count(c => c.Status == PartnershipStatus.Pending);

        var carrierIds = connections.Select(c => c.CarrierId).Distinct().ToList();
        var freeVehicleCounts = await _vehicles.CountFreeByCarrierIdsAsync(carrierIds, cancellationToken);

        var partners = connections
            .Select(connection => BuildDto(connection, freeVehicleCounts))
            .ToList();

        return new PartnerNetworkDto(totalActive, totalPending, partners);
    }

    private static PartnerDto BuildDto(PartnerConnection connection, Dictionary<Guid, int> freeVehicleCounts)
    {
        var carrier = connection.Carrier;

        var carrierDto = new CarrierSummaryDto(
            carrier?.Id ?? connection.CarrierId,
            carrier?.TradeName ?? carrier?.CompanyName ?? "(transportadora não encontrada)",
            carrier is not null ? CnpjFormatter.Format(carrier.Cnpj) : string.Empty,
            carrier?.AverageRating,
            carrier?.HasActiveInsurancePolicy ?? false);

        var freeVehicles = freeVehicleCounts.GetValueOrDefault(connection.CarrierId, 0);

        // TODO: viagensAtivasConosco depende da entidade Viagem/Trip, que ainda não existe
        // no domínio. Fixo em 0 até essa entidade existir.
        var metrics = new OperationalMetricsDto(freeVehicles, 0, connection.UpdatedAt);

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