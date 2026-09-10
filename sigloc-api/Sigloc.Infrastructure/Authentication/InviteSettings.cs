namespace Sigloc.Infrastructure.Authentication;

public class InviteSettings
{
    // Keeps the appsettings.json section name tied to this class.
    public const string SectionName = "Invite";

    /// <summary>
    /// Base URL of the frontend used to build the shareable invite link, e.g.
    /// "https://app.sigloc.com/convite". The token is appended to this path.
    /// </summary>
    public string FrontendBaseUrl { get; init; } = string.Empty;

    /// <summary>Default lifetime (in days) applied when the caller omits an expiry.</summary>
    public int DefaultExpiryDays { get; init; } = 7;
}
