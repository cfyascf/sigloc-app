using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Carrier company (Transportadora). Onboarded through the smart invite flow.
/// Holds the public company data, kept separate from the access credentials
/// stored in <see cref="User"/>.
/// </summary>
[Table("Carrier")]
public class Carrier : BaseEntity
{
    /// <summary>Brazilian company tax id (CNPJ). Unique across the platform.</summary>
    public required string Cnpj { get; set; }

    /// <summary>Legal company name (razão social).</summary>
    public required string CompanyName { get; set; }

    /// <summary>Trade name (nome fantasia).</summary>
    public string? TradeName { get; set; }

    /// <summary>Average rating (1-5) given by contractors. Null until the carrier has at least one review.</summary>
    public double? AverageRating { get; set; }

    /// <summary>Whether the carrier currently has an active cargo insurance policy on file.</summary>
    public bool HasActiveInsurancePolicy { get; set; }
}
