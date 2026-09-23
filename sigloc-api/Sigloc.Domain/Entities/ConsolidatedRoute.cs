using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Consolidated route (Rota Consolidada): the physical and financial snapshot of a
/// group of route segments (Trechos) taken at the moment the auction is opened.
/// Created atomically together with its <see cref="Auction"/>.
/// </summary>
[Table("ConsolidatedRoute")]
public class ConsolidatedRoute : BaseEntity
{
    /// <summary>Owning contractor (Contratante).</summary>
    public Guid ContractorId { get; set; }

    public ConsolidatedRouteStatus Status { get; set; } = ConsolidatedRouteStatus.Planned;

    /// <summary>Sum of each segment's own distance plus the inter-segment legs (km).</summary>
    public double TotalDistanceKm { get; set; }

    /// <summary>Sum of each segment's own duration plus the inter-segment legs (hours).</summary>
    public double EstimatedTimeHours { get; set; }

    /// <summary>Sum of the "Orçamento Teto" of every linked segment.</summary>
    public decimal ConsolidatedBudgetCeiling { get; set; }

    /// <summary>Estimated minimum freight (ANTT floor) used to block abusive bids.</summary>
    public decimal EstimatedAnttFloor { get; set; }

    /// <summary>Snapshot (sum) of the weight of every linked segment.</summary>
    public double TotalWeightKg { get; set; }

    /// <summary>Snapshot (sum) of the volume of every linked segment.</summary>
    public double TotalVolumeM3 { get; set; }
}
