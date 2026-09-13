namespace Sigloc.Domain.Enums;

/// <summary>
/// Lifecycle of a partnership connection between a contractor and a carrier.
/// </summary>
public enum PartnershipStatus
{
    /// <summary>Awaiting the carrier's confirmation (e.g. carrier already existed and must log in to accept).</summary>
    Pending,

    /// <summary>Active partnership. Created automatically when a brand-new carrier onboards through an invite.</summary>
    Active
}
