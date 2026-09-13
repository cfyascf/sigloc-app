using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace Sigloc.Infrastructure.Authentication;

/// <summary>
/// Verifies Google ID tokens against Google's public keys, checking the signature,
/// expiry and that the audience matches the configured client id.
/// </summary>
public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly GoogleAuthSettings _settings;

    public GoogleTokenVerifier(IOptions<GoogleAuthSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new InvalidGoogleTokenException();
        }

        var settings = new ValidationSettings();
        if (!string.IsNullOrWhiteSpace(_settings.ClientId))
        {
            settings.Audience = new[] { _settings.ClientId };
        }

        Payload payload;
        try
        {
            payload = await ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            throw new InvalidGoogleTokenException(ex);
        }

        return new GoogleUserInfo(
            payload.Subject,
            payload.Email,
            payload.EmailVerified,
            payload.Name);
    }
}
