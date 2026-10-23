using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Shipper company (Contratante). Holds the public company data, kept separate
/// from the access credentials stored in <see cref="User"/>.
/// </summary>
[Table("Contractor")]
public class Contractor : BaseEntity
{
    /// <summary>Brazilian company tax id (CNPJ). Unique across the platform.</summary>
    public required string Cnpj { get; set; }

    /// <summary>Legal company name (razão social).</summary>
    public required string CompanyName { get; set; }

    /// <summary>Trade name (nome fantasia).</summary>
    public string? TradeName { get; set; }

    /// <summary>Person or desk carriers should reach about trips in progress.</summary>
    public string? LogisticsContactName { get; set; }

    /// <summary>Logistics desk phone, stored as entered (validated to 10–13 digits).</summary>
    public string? LogisticsContactPhone { get; set; }

    /// <summary>Logistics desk e-mail.</summary>
    public string? LogisticsContactEmail { get; set; }
}
