using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Physical transport cycle (Viagem) created when an auction is awarded. Links the
/// consolidated route to the winning carrier, vehicle and bid, starting the operation.
/// </summary>
[Table("Trip")]
public class Trip : BaseEntity
{
    /// <summary>Consolidated route (Rota Consolidada) being transported.</summary>
    public Guid RouteId { get; set; }

    /// <summary>Auction (Leilão) that originated the trip.</summary>
    public Guid AuctionId { get; set; }

    /// <summary>Winning carrier (Transportadora).</summary>
    public Guid CarrierId { get; set; }

    /// <summary>Winning vehicle (Veículo) allocated to the trip.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Winning bid (Lance) that closed the auction.</summary>
    public Guid BidId { get; set; }

    /// <summary>Agreed total freight value taken from the winning bid.</summary>
    public decimal AgreedValue { get; set; }

    public TripStatus Status { get; set; } = TripStatus.AwaitingPickup;
}
