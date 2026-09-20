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