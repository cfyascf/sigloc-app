namespace Sigloc.Application.DTOs;

// --- POST /api/rotas/preview -------------------------------------------------------

public record RoutePreviewRequestDto(IReadOnlyList<Guid>? TrechoIds);

public record TotaisAgregadosDto(double PesoTotalKg, double VolumeTotalM3);

/// <summary>
/// Read-only simulation of a consolidated route. Nothing is written to the database;
/// the same numbers are recomputed (and this time persisted) by POST /api/leiloes.
/// </summary>
public record RoutePreviewResponseDto(
    double DistanciaTotalKm,
    double TempoEstimadoHoras,
    decimal TetoConsolidado,
    decimal PisoAnttEstimado,
    decimal PedagioPrevisto,
    decimal CustoRodagemKm,
    TotaisAgregadosDto TotaisAgregados,
    string ExigenciaVeiculoConsolidada);

// --- POST /api/leiloes --------------------------------------------------------------

public record CreateAuctionRequestDto(
    IReadOnlyList<Guid>? TrechoIds,
    DateTimeOffset? ExpiraEm,
    bool? AdjudicacaoAutomatica);

public record AuctionDto(
    Guid Id,
    Guid RotaId,
    DateTimeOffset AbertoEm,
    DateTimeOffset ExpiraEm,
    bool AdjudicacaoAutomatica,
    string Status);

public record ConsolidatedRouteSummaryDto(
    Guid Id,
    string Status,
    double DistanciaTotalKm,
    double TempoEstimadoHoras,
    double PesoTotalKg,
    double VolumeTotalM3,
    decimal TetoConsolidado,
    decimal PisoAnttEstimado);

public record CreateAuctionResponseDto(
    AuctionDto Leilao,
    ConsolidatedRouteSummaryDto RotaConsolidada,
    int TrechosAtualizados);
