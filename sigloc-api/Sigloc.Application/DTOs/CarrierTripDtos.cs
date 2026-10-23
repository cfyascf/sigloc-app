using System.Text.Json.Serialization;
namespace Sigloc.Application.DTOs;

/// <summary>Transport-agnostic upload so the application layer never depends on ASP.NET's IFormFile.</summary>
public sealed record UploadedFileDto(Stream Content, string? FileName, long Length);

/// <summary>Request body for POST /api/transportador/viagens/{id}/ocorrencias.</summary>
public sealed record ReportTripOccurrenceRequestDto(
    [property: JsonPropertyName("tipo")] string? Type,
    [property: JsonPropertyName("observacao")] string? Note);

/// <summary>
/// Carrier view of an active trip: the shared monitoring detail (<see cref="Trip"/>) plus the
/// operational context the driver needs to act on it.
/// </summary>
public sealed record CarrierTripDetailDto(
    [property: JsonPropertyName("viagem")] TripDetailDto Trip,
    [property: JsonPropertyName("embarcador")] TripShipperContactDto? Shipper,
    [property: JsonPropertyName("especificacaoCarga")] TripCargoSpecsDto Cargo,
    [property: JsonPropertyName("acaoAtual")] TripCurrentActionDto? CurrentAction,
    [property: JsonPropertyName("ocorrencias")] IReadOnlyList<TripOccurrenceDto> Occurrences,
    [property: JsonPropertyName("comprovantes")] IReadOnlyList<TripProofOfDeliveryDto> Proofs);

public sealed record TripShipperContactDto(
    [property: JsonPropertyName("razaoSocial")] string CompanyName,
    [property: JsonPropertyName("nomeFantasia")] string? TradeName,
    [property: JsonPropertyName("nomeContato")] string? ContactName,
    [property: JsonPropertyName("telefone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email);

public sealed record TripCargoSpecsDto(
    [property: JsonPropertyName("ambiente")] string Environment,
    [property: JsonPropertyName("refrigerada")] bool Refrigerated,
    [property: JsonPropertyName("temperaturaMinima")] double? TempMin,
    [property: JsonPropertyName("temperaturaMaxima")] double? TempMax,
    [property: JsonPropertyName("paletizada")] bool Palletized,
    [property: JsonPropertyName("embalagens")] IReadOnlyList<string> Packagings,
    [property: JsonPropertyName("perigosa")] bool Dangerous,
    [property: JsonPropertyName("fragil")] bool Fragile,
    [property: JsonPropertyName("carroceria")] string Bodywork,
    [property: JsonPropertyName("pesoTotalKg")] double? TotalWeightKg,
    [property: JsonPropertyName("volumeTotalM3")] double? TotalVolumeM3,
    [property: JsonPropertyName("produtos")] IReadOnlyList<TripCargoProductDto> Products);

public sealed record TripCargoProductDto(
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("quantidade")] int Quantity);

/// <summary>The only stop the carrier may confirm next, and whether confirming it needs a POD.</summary>
public sealed record TripCurrentActionDto(
    [property: JsonPropertyName("paradaId")] Guid StopId,
    [property: JsonPropertyName("tipo")] string Kind,
    [property: JsonPropertyName("exigeComprovante")] bool RequiresProof,
    [property: JsonPropertyName("prazo")] DateTimeOffset? Deadline);

public sealed record TripOccurrenceDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tipo")] string Type,
    [property: JsonPropertyName("rotulo")] string Label,
    [property: JsonPropertyName("severidade")] string Severity,
    [property: JsonPropertyName("observacao")] string? Note,
    [property: JsonPropertyName("reportadaEm")] DateTimeOffset ReportedAt,
    [property: JsonPropertyName("paradaId")] Guid? StopId,
    [property: JsonPropertyName("encerradaEm")] DateTimeOffset? ResolvedAt)
{
    [JsonPropertyName("aberta")] public bool IsOpen => ResolvedAt == null;
}

public sealed record TripProofOfDeliveryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("paradaId")] Guid StopId,
    [property: JsonPropertyName("nomeArquivo")] string FileName,
    [property: JsonPropertyName("tipoConteudo")] string ContentType,
    [property: JsonPropertyName("tamanhoBytes")] long SizeBytes,
    [property: JsonPropertyName("enviadoEm")] DateTimeOffset UploadedAt);

/// <summary>An opened proof-of-delivery file ready to stream back to the carrier.</summary>
public sealed record ProofOfDeliveryFileDto(Stream Content, string ContentType, string FileName);
