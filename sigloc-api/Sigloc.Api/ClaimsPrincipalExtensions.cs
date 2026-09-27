using System.Security.Claims;

namespace Sigloc.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the company id (empresaId) from the authenticated user's JWT claims.
    /// The claim holds the id of the Transportadora/Contratante the user belongs to.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when the claim is missing or not a valid Guid.</exception>
    public static Guid GetCompanyId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("empresaId");
        if (Guid.TryParse(raw, out var companyId))
        {
            return companyId;
        }

        throw new UnauthorizedAccessException("O token não contém uma empresa (empresaId) válida.");
    }
}
