using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

[Table("Product")]
public class Product : BaseEntity
{
    /// <summary>Owning contractor (Contratante). Products are scoped per contractor.</summary>
    public Guid ContractorId { get; set; }

    /// <summary>Stock keeping unit. Unique per contractor.</summary>
    public required string Sku { get; set; }

    public required string Name { get; set; }

    /// <summary>Free-form segment (e.g. "Alimentício"). Informative only.</summary>
    public string? Type { get; set; }

    public ProductCategory Category { get; set; }

    public TransportEnvironment TransportEnvironment { get; set; }

    /// <summary>Minimum temperature (°C). Only relevant when the environment is not Dry.</summary>
    public double? TempMin { get; set; }

    /// <summary>Maximum temperature (°C). Only relevant when the environment is not Dry.</summary>
    public double? TempMax { get; set; }

    /// <summary>Packaging type. Only relevant when the category is General.</summary>
    public PackagingType? PackagingType { get; set; }

    public bool Dangerous { get; set; }

    public bool Fragile { get; set; }

    /// <summary>Default weight (kg). Must be greater than zero.</summary>
    public double DefaultWeight { get; set; }

    /// <summary>Default volume (m³). Must be greater than zero.</summary>
    public double DefaultVolume { get; set; }

    /// <summary>Free-form handling note. Informative only.</summary>
    public string? HandlingRestriction { get; set; }
}
