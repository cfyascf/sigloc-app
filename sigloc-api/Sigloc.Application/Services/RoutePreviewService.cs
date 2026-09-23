using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;

namespace Sigloc.Application.Services;

public class RoutePreviewService : IRoutePreviewService
{
    private readonly RouteSegmentAggregator _aggregator;

    public RoutePreviewService(RouteSegmentAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public async Task<RoutePreviewResponseDto> PreviewAsync(Guid contractorId, RoutePreviewRequestDto dto, CancellationToken cancellationToken = default)
    {
        var segmentIds = ValidateSegmentIds(dto.TrechoIds);

        var (_, aggregation) = await _aggregator.LoadAndAggregateAsync(contractorId, segmentIds, asTracking: false, cancellationToken);

        return MapToResponse(aggregation);
    }

    internal static IReadOnlyList<Guid> ValidateSegmentIds(IReadOnlyList<Guid>? trechoIds)
    {
        if (trechoIds is null || trechoIds.Count == 0)
        {
            throw new ValidationException(
                new[] { new ValidationError("trechoIds", "Obrigatório. Informe pelo menos um trecho.") },
                "Não foi possível processar a rota.");
        }

        return trechoIds.Distinct().ToList();
    }

    internal static RoutePreviewResponseDto MapToResponse(RouteAggregationResult aggregation)
    {
        var (pisoAnttEstimado, custoRodagemKm) = RouteFinancials.EstimateAnttFloor(aggregation.TotalDistanceKm);

        return new RoutePreviewResponseDto(
            DistanciaTotalKm: aggregation.TotalDistanceKm,
            TempoEstimadoHoras: aggregation.TotalTimeHours,
            TetoConsolidado: aggregation.ConsolidatedBudgetCeiling,
            PisoAnttEstimado: pisoAnttEstimado,
            PedagioPrevisto: aggregation.EstimatedTollCost,
            CustoRodagemKm: custoRodagemKm,
            TotaisAgregados: new TotaisAgregadosDto(aggregation.TotalWeightKg, aggregation.TotalVolumeM3),
            ExigenciaVeiculoConsolidada: RouteFinancials.DescribeVehicleRequirement(aggregation.VehicleRequirement));
    }
}

/// <summary>
/// Small helpers shared by the preview and auction creation responses that are not
/// backed by real cost data yet.
/// </summary>
internal static class RouteFinancials
{
    // Rough national-average placeholder rate (R$/km), NOT the official ANTT minimum
    // freight table (which depends on axle count, cargo type and load direction).
    // TODO: replace with a real ANTT floor calculation — tracked as future work, see
    // the note in api-mappings/api-mapping-create-route-segment.md.
    private const decimal PlaceholderRodagemRatePerKm = 8.33m;

    public static (decimal PisoAnttEstimado, decimal CustoRodagemKm) EstimateAnttFloor(double totalDistanceKm)
    {
        var floor = Math.Round(PlaceholderRodagemRatePerKm * (decimal)totalDistanceKm, 2);
        return (floor, PlaceholderRodagemRatePerKm);
    }

    public static string DescribeVehicleRequirement(ConsolidatedVehicleRequirement requirement)
        => requirement.MinRefrigerationLevel switch
        {
            "Congelado" => "FRIGORÍFICO",
            "Resfriado" => "REFRIGERADO",
            _ => requirement.BaseBodyworkType.ToUpperInvariant()
        };
}
