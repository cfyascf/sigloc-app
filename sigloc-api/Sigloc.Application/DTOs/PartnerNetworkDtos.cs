using System.Text.Json.Serialization;

namespace Sigloc.Application.DTOs;

public record CarrierSummaryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("nomeFantasia")] string TradeName,
    [property: JsonPropertyName("cnpj")] string Cnpj,
    [property: JsonPropertyName("notaMedia")] double? AverageRating,
    [property: JsonPropertyName("apoliceSeguroAtiva")] bool HasActiveInsurancePolicy);

public record OperationalMetricsDto(
    [property: JsonPropertyName("veiculosLivres")] int FreeVehicles,
    [property: JsonPropertyName("viagensAtivasConosco")] int ActiveTripsWithUs,
    [property: JsonPropertyName("ultimaInteracao")] DateTimeOffset? LastInteraction);

public record PartnerDto(
    [property: JsonPropertyName("conexaoId")] Guid ConnectionId,
    [property: JsonPropertyName("statusParceria")] string PartnershipStatus,
    [property: JsonPropertyName("transportadora")] CarrierSummaryDto Carrier,
    [property: JsonPropertyName("metricasOperacionais")] OperationalMetricsDto OperationalMetrics);

public record PartnerNetworkDto(
    [property: JsonPropertyName("totalAtivos")] int TotalActive,
    [property: JsonPropertyName("totalPendentes")] int TotalPending,
    [property: JsonPropertyName("parceiros")] List<PartnerDto> Partners);

public record ContractorSummaryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("nomeFantasia")] string TradeName,
    [property: JsonPropertyName("cnpj")] string Cnpj);

/// <param name="OpenOffers">The contractor's auctions open to bids right now (0 unless the partnership is active).</param>
/// <param name="ActiveLanes">Distinct origin → destination lanes the carrier runs for this contractor on trips awaiting pickup or in transit.</param>
public record CarrierNetworkPartnerDto(
    [property: JsonPropertyName("conexaoId")] Guid ConnectionId,
    [property: JsonPropertyName("statusParceria")] string PartnershipStatus,
    [property: JsonPropertyName("contratante")] ContractorSummaryDto Contractor,
    [property: JsonPropertyName("ultimaInteracao")] DateTimeOffset? LastInteraction,
    [property: JsonPropertyName("ofertasAbertas")] int OpenOffers = 0,
    [property: JsonPropertyName("corredoresAtivos")] int ActiveLanes = 0);

public record CarrierNetworkDto(
    [property: JsonPropertyName("totalAtivos")] int TotalActive,
    [property: JsonPropertyName("totalPendentes")] int TotalPending,
    [property: JsonPropertyName("parceiros")] List<CarrierNetworkPartnerDto> Partners);

/// <summary>Resposta para POST /api/auth/invite/{token}/connect.</summary>
public record PartnerConnectionCreatedDto(
    [property: JsonPropertyName("conexaoId")] Guid ConnectionId,
    [property: JsonPropertyName("contratante")] string ContractorName,
    [property: JsonPropertyName("statusParceria")] string PartnershipStatus);