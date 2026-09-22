using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Partnership link (CONEXAO_PARCERIA) between a contractor and a carrier.
/// Created during smart onboarding, either active (brand-new carrier) or pending
/// (existing carrier that must log in to accept).
/// </summary>
[Table("PartnerConnection")]
public class PartnerConnection : BaseEntity
{
    /// <summary>Contractor side of the partnership.</summary>
    public Guid ContractorId { get; set; }

    /// <summary>Carrier side of the partnership.</summary>
    public Guid CarrierId { get; set; }

    /// <summary>Current lifecycle state of the partnership.</summary>
    public PartnershipStatus Status { get; set; }

    /// <summary>Which side started this connection (iniciadoPor no diagrama).</summary>
    public PartnershipInitiator InitiatedBy { get; set; }

    /// <summary>Navigation property - só vem preenchida quando o repositório faz .Include(c => c.Carrier).</summary>
    public Carrier? Carrier { get; set; }
}
