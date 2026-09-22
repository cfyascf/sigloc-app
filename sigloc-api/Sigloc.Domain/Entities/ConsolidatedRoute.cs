using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Physical and financial snapshot of a consolidated trip (Rota Consolidada). Groups
/// several route segments and stores the aggregated totals so the auction engine can
/// query them efficiently. The snapshot is written atomically when the auction starts.
/// </summary>
[Table("ConsolidatedRoute")]
public class ConsolidatedRoute : BaseEntity
{
    /// <summary>Owning contractor (Contratante).</summary>
    public Guid ContractorId { get; set; }

    public RouteStatus Status { get; set; } = RouteStatus.Planned;

    /// <summary>Sum of the linked segment distances plus the inter-segment distances (km).</summary>
    public double TotalDistanceKm { get; set; }

    /// <summary>ETA for the whole route (hours), including inter-segment legs.</summary>
    public double EstimatedTimeHours { get; set; }

    /// <summary>Sum of the budget ceilings of all linked segments.</summary>
    public decimal ConsolidatedCeiling { get; set; }

    /// <summary>Estimated minimum freight (ANTT floor) used to block abusive bids.</summary>
    public decimal EstimatedAnttFloor { get; set; }

    /// <summary>Snapshot of the total weight of the linked segments (kg).</summary>
    public double TotalWeightKg { get; set; }

    /// <summary>Snapshot of the total volume of the linked segments (m³).</summary>
    public double TotalVolumeM3 { get; set; }
}
