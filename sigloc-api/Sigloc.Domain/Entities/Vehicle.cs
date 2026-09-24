using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

[Table("Vehicle")]
public class Vehicle : BaseEntity
{
    /// <summary>Owning carrier (Transportadora). Vehicles are scoped per carrier.</summary>
    public Guid CarrierId { get; set; }

    /// <summary>License plate. Normalized (uppercase, no separators) and unique per carrier.</summary>
    public required string Plate { get; set; }

    public required string Model { get; set; }

    /// <summary>Number of axles. Used for ANTT/toll calculation. Must be greater than zero.</summary>
    public int AxleCount { get; set; }

    /// <summary>Weight capacity (kg). Must be greater than zero.</summary>
    public decimal CapacityWeight { get; set; }

    /// <summary>Volume capacity (m³). Must be greater than zero.</summary>
    public decimal CapacityVolume { get; set; }

    public VehicleBodyType BodyType { get; set; }

    public RefrigerationLevel RefrigerationLevel { get; set; }

    /// <summary>Whether the vehicle carries MOPP-certified handling for dangerous cargo.</summary>
    public bool HasMopp { get; set; }

    /// <summary>Whether the vehicle has cargo securing equipment.</summary>
    public bool HasCargoSecuring { get; set; }

    /// <summary>Assigned driver. Informative only.</summary>
    public string? Driver { get; set; }

    /// <summary>Current known location. Informative only.</summary>
    public string? CurrentLocation { get; set; }

    public OperationalStatus Status { get; set; } = OperationalStatus.LIVRE;
}
