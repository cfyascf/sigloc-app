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
        string? DriverPhone = null,
        VehicleDocumentsDto? Documents = null
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
        string? DriverPhone = null,
        // Null leaves the documents as they are; inside, a null field keeps its stored value.
        VehicleDocumentsDto? Documents = null
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
        bool OnActiveTrip = false,
        VehicleDocumentsDto? Documents = null,
        // Missing (warning) or invalid (blocking) documents, worked out with the carrier's RNTRC.
        IReadOnlyList<ComplianceIssueDto>? Compliance = null
    );

    /// <summary>
    /// Documents for running paid freight: the vehicle's composition and RNTRC (ANTT) and its
    /// driver's CNH, plus MOPP and CIV/CIPP validity. Enums travel as their integer values and
    /// dates as "yyyy-MM-dd".
    /// </summary>
    public record VehicleDocumentsDto(
        VehicleComposition? Composition = null,
        RntrcSource? RntrcSource = null,
        string? RntrcNumber = null,
        RntrcCategory? RntrcCategory = null,
        string? RntrcHolderName = null,
        string? RntrcHolderDocument = null,
        DateOnly? RntrcValidUntil = null,
        CnhCategory? DriverCnhCategory = null,
        bool? DriverHasEar = null,
        DateOnly? DriverToxicologicalExamValidUntil = null,
        DateOnly? MoppValidUntil = null,
        DateOnly? CivCippValidUntil = null);

    /// <summary>One document problem. Severity is "WARNING" (shown only) or "BLOCKING" (stops bids).</summary>
    public record ComplianceIssueDto(string Code, string Severity, string Message);
}