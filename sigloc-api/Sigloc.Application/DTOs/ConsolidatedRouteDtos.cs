using Sigloc.Domain.Constants;

namespace Sigloc.Application.DTOs;

public record CreateConsolidatedRouteDto(
    Guid ContratanteId,
    EnumRotaConsolidada Status, 
    double DistanciaTotalKm, 
    double TempoEstimadoHoras, 
    double TetoConsolidado, 
    double PisoAnttEstimado, 
    double PesoTotalKg,  
    double VolumeTotalM3
);
public record UpdateConsolidatedRouteDto(
    Guid ContratanteId,
    EnumRotaConsolidada Status, 
    double DistanciaTotalKm, 
    double TempoEstimadoHoras, 
    double TetoConsolidado, 
    double PisoAnttEstimado, 
    double PesoTotalKg,  
    double VolumeTotalM3
);
public record ConsolidatedRouteResponseDto(
    Guid Id,
    Guid ContratanteId,
    EnumRotaConsolidada Status, 
    double DistanciaTotalKm, 
    double TempoEstimadoHoras, 
    double TetoConsolidado, 
    double PisoAnttEstimado, 
    double PesoTotalKg,  
    double VolumeTotalM3
);
