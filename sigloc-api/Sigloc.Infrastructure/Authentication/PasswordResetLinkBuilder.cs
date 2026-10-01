using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Infrastructure.Configurations;

namespace Sigloc.Infrastructure.Authentication;

public class PasswordResetLinkBuilder : IPasswordResetLinkBuilder
{
    private readonly PasswordResetSettings _settings;

    public PasswordResetLinkBuilder(IOptions<PasswordResetSettings> settings)
    {
        _settings = settings.Value;
    }

    public int TokenExpiryMinutes => _settings.TokenExpiryMinutes;

    public string Build(string token)
    {
        var separator = _settings.FrontendBaseUrl.Contains('?') ? '&' : '?';
        return $"{_settings.FrontendBaseUrl}{separator}token={Uri.EscapeDataString(token)}";
    }
}
