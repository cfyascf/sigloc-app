using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Operational transport unit (Trecho): a geographic origin → destination leg that
/// links products to an itinerary. Physical totals (weight, volume) and the
/// consolidated vehicle requirement are never stored; they are computed on demand
/// from the linked <see cref="ProductRouteSegment"/> rows.
/// </summary>
[Table("RouteSegment")]
public class RouteSegment : BaseEntity
{
    /// <summary>Owning contractor (Contratante).</summary>
    public Guid ContractorId { get; set; }

    /// <summary>Consolidated route. Null on creation; set once the segment is consolidated.</summary>
    public Guid? RouteId { get; set; }

    /// <summary>Pickup location text (e.g. "Curitiba, PR").</summary>
    public required string OriginAddress { get; set; }

    /// <summary>Delivery location text (e.g. "São Paulo, SP").</summary>
    public required string DestinationAddress { get; set; }

    /// <summary>Total distance (km). Populated from the OpenRouteService matrix. Greater than zero.</summary>
    public double DistanceKm { get; set; }

    /// <summary>Estimated travel time (hours). Populated from the OpenRouteService matrix. Greater than zero.</summary>
    public double EstimatedTimeHours { get; set; }

    /// <summary>Origin coordinate as "longitude,latitude". Populated via OpenRouteService.</summary>
    public required string OriginCoordinate { get; set; }

    /// <summary>Destination coordinate as "longitude,latitude". Populated via OpenRouteService.</summary>
    public required string DestinationCoordinate { get; set; }

    /// <summary>Guide budget for the segment (optional).</summary>
    public decimal? BudgetCeiling { get; set; }

    /// <summary>Estimated toll cost for executing the segment.</summary>
    public decimal EstimatedTollCost { get; set; }

    /// <summary>Deadline to pick up the cargo.</summary>
    public DateTimeOffset PickupDeadline { get; set; }

    /// <summary>Deadline to complete delivery.</summary>
    public DateTimeOffset DeliveryDeadline { get; set; }

    public SegmentStatus Status { get; set; } = SegmentStatus.Available;

    /// <summary>Products linked to this segment through the associative table.</summary>
    public List<ProductRouteSegment> Items { get; set; } = new();
}
