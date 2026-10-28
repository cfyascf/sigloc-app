using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sigloc.Application.Contracts;

namespace Sigloc.Infrastructure.Authentication;

public sealed class JwtProvider : IJwtProvider
{
    private readonly JwtSettings _jwtSettings;

    public JwtProvider(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <summary>Claim holding <see cref="Sigloc.Domain.Entities.User.TokenVersion"/>; checked on every request.</summary>
    public const string TokenVersionClaim = "versaoToken";

    public string Generate(Guid userId, string email, string role, string profileType, Guid? companyId, int tokenVersion = 0)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("tipoPerfil", profileType),
            new(TokenVersionClaim, tokenVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer32)
        };

        if (companyId.HasValue)
        {
            claims.Add(new Claim("empresaId", companyId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
