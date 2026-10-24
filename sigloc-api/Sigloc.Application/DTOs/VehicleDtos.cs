using System;
using Sigloc.Domain.Enums; 

namespace Sigloc.Application.DTOs
{
    public record CreateVehicleDto(
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
        long? TraccarDeviceId = null,
        string? DriverPhone = null
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
        OperationalStatus? Status,
        long? TraccarDeviceId = null,
        string? DriverPhone = null
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
        OperationalStatus Status,
        long? TraccarDeviceId = null,
        string? DriverPhone = null,
        // True while the vehicle is committed to a trip awaiting pickup or in transit. Kept apart
        // from Status, which is the carrier-managed value round-tripped by the edit form.
        bool OnActiveTrip = false
    );
}