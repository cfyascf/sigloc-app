namespace Sigloc.Domain.Enums;

/// <summary>
/// Access profile that drives role-based access control (RBAC). Persisted and
/// carried inside the JWT so the front-end can route the user to the right
/// dashboard.
/// </summary>
public enum ProfileType
{
    /// <summary>Shipper / logistics operator (Contratante). Open self-service registration.</summary>
    Contratante,

    /// <summary>Carrier (Transportador). Joins through the smart invite flow.</summary>
    Transportador,

    /// <summary>System administrator. Seeded directly, has global privileges, no company.</summary>
    Admin
}
