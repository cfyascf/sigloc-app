using Sigloc.Domain.Enums;

namespace Sigloc.Application.DTOs;

public record TravelMacroResponseDto(
    Guid ViagemId,
    TravelStatus StatusGeral,
    decimal ProgressoPercentual,
    DateTimeOffset? UltimoEtaCalculado,
    DateTimeOffset? UltimaAtualizacaoPing
);

public record TravelDetalheResponseDto(
    Guid ViagemId,
    TravelStatus StatusGeral,
    decimal ProgressoPercentual,
    DateTimeOffset? NovoEta,
    bool IsCacheRenovado,
    double? UltimaLatitude,
    double? UltimaLongitude
);