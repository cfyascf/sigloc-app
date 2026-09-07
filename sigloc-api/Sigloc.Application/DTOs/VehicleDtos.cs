using System;
using Sigloc.Domain.Enums; 

namespace Sigloc.Application.DTOs
{
    public record CreateVehicleDto(
        Guid TransportadoraId,
        string Plate, 
        string Model, 
        int AxleCount,
        decimal CapacityWeight, 
        decimal CapacityVolume, 
        VehicleBodyType BodyType,
        RefrigerationLevel RefrigerationLevel,
        bool HasMopp,
        bool HasCargoSecuring,
        string Driver,
        string CurrentLocation
    );

    public record UpdateVehicleDto(
        string? Model, 
        int? AxleCount,
        decimal? CapacityWeight, 
        decimal? CapacityVolume, 
        VehicleBodyType? BodyType,
        RefrigerationLevel? RefrigerationLevel,
        bool? HasMopp,
        bool? HasCargoSecuring,
        string? Driver,
        string? CurrentLocation,
        OperationalStatus? Status
    );

    public record VehicleResponseDto(
        Guid Id, 
        Guid TransportadoraId,
        string Plate, 
        string Model, 
        int AxleCount,
        decimal CapacityWeight, 
        decimal CapacityVolume, 
        VehicleBodyType BodyType,
        RefrigerationLevel RefrigerationLevel,
        bool HasMopp,
        bool HasCargoSecuring,
        string Driver,
        string CurrentLocation,
        OperationalStatus Status
    );
}