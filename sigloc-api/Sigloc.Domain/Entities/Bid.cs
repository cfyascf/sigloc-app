using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Bid (Lance) submitted by a carrier against an auction. Ranking is driven by
/// <see cref="TotalValue"/> (net freight plus toll); ties are broken by the earliest
/// <see cref="SubmittedAt"/>.
/// </summary>
[Table("Bid")]
public class Bid : BaseEntity
{
    /// <summary>Auction (Leilão) the bid competes in.</summary>
    public Guid AuctionId { get; set; }

    /// <summary>Carrier (Transportadora) that submitted the bid.</summary>
    public Guid CarrierId { get; set; }

    /// <summary>Vehicle (Veículo) allocated to the bid; used for axle/toll math and anti-overbooking.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Net freight value the carrier set for itself.</summary>
    public decimal NetFreightValue { get; set; }

    /// <summary>Toll cost computed by the back-end (route toll × vehicle axles).</summary>
    public decimal TollValue { get; set; }

    /// <summary>Total value charged to the shipper (net freight + toll). Defines the ranking.</summary>
    public decimal TotalValue { get; set; }

    /// <summary>Exact submission timestamp. Tiebreaker: earliest wins the higher rank.</summary>
    public DateTimeOffset SubmittedAt { get; set; }

    public BidStatus Status { get; set; } = BidStatus.Pending;
}
