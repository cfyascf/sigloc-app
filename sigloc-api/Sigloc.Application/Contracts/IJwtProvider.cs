namespace Sigloc.Application.Contracts;

public interface IJwtProvider
{
    /// <summary>
    /// Issues a signed JWT carrying the user identity, RBAC role, the raw profile
    /// type and the owning company id (null for admins).
    /// </summary>
    string Generate(Guid userId, string email, string role, string profileType, Guid? companyId);
}
