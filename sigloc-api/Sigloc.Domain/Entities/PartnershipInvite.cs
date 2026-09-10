using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Smart invite (Convite Inteligente) issued by a contractor to bring a carrier
/// into the platform through a B2B link.
/// </summary>
[Table("PartnershipInvite")]
public class PartnershipInvite : BaseEntity
{
    /// <summary>Opaque token embedded in the invite link. Unique across the platform.</summary>
    public required string Token { get; set; }

    /// <summary>Contractor that owns (issued) the invite.</summary>
    public Guid ContractorId { get; set; }

    /// <summary>Optional e-mail the invite was addressed to (for the contractor's records).</summary>
    public string? InviteeEmail { get; set; }

    /// <summary>Optional expiry. When null, the invite does not expire.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Whether the invite has already been consumed by a carrier onboarding.</summary>
    public bool IsUsed { get; set; }
}
