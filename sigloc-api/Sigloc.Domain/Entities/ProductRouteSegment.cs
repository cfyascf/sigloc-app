using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Associative row (PRODUTO_TRECHO) linking a product to a route segment along with
/// the quantity carried. Physical subtotals are computed on demand from the linked
/// product and this quantity; they are never stored.
/// </summary>
[Table("ProductRouteSegment")]
public class ProductRouteSegment
{
    public Guid Id { get; set; }

    /// <summary>Owning route segment.</summary>
    public Guid RouteSegmentId { get; set; }

    /// <summary>Linked product.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Quantity of the product carried on this segment. Must be greater than zero.</summary>
    public int Quantity { get; set; }

    /// <summary>Navigation to the linked product. Loaded when computing totals/requirements.</summary>
    public Product? Product { get; set; }
}
