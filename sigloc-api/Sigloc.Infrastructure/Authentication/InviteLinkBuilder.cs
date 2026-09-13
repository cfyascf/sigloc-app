using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;

namespace Sigloc.Infrastructure.Authentication;

public sealed class InviteLinkBuilder : IInviteLinkBuilder
{
    private readonly InviteSettings _settings;

    public InviteLinkBuilder(IOptions<InviteSettings> options)
    {
        _settings = options.Value;
    }

    public string Build(string token)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(baseUrl) ? token : $"{baseUrl}/{token}";
    }
}
