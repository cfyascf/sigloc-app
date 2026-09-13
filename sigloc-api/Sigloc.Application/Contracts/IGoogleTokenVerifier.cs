namespace Sigloc.Application.Contracts;

/// <summary>Verified identity extracted from a Google ID token.</summary>
public record GoogleUserInfo(string Subject, string Email, bool EmailVerified, string? Name);

/// <summary>
/// Verifies a Google ID token (signature, issuer, audience and expiry) and
/// returns the trusted identity it carries.
/// </summary>
public interface IGoogleTokenVerifier
{
    /// <summary>
    /// Validates the supplied Google ID token. Returns the verified identity, or
    /// throws when the token is invalid.
    /// </summary>
    Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}
