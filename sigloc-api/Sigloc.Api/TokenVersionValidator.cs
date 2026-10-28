using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Authentication;

namespace Sigloc.Api.Extensions;

/// <summary>
/// Rejects a signed, unexpired token whose version is older than the user's current one (the user
/// logged out or reset the password since it was issued), or whose user no longer exists. Tokens
/// issued before versioning carry no claim and count as version 0.
/// </summary>
public static class TokenVersionValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Fail("Token sem identificação do usuário.");
            return;
        }

        var claimed = int.TryParse(principal!.FindFirstValue(JwtProvider.TokenVersionClaim), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out var version) ? version : 0;

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var current = await users.GetTokenVersionAsync(userId, context.HttpContext.RequestAborted);
        if (current != claimed)
        {
            context.Fail("Sessão encerrada. Entre novamente.");
        }
    }
}
