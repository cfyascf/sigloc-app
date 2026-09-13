namespace Sigloc.Infrastructure.Authentication;

public class GoogleAuthSettings
{
    // Keeps the appsettings.json section name tied to this class.
    public const string SectionName = "GoogleAuth";

    /// <summary>
    /// OAuth 2.0 client id issued by Google. The ID token's audience must match
    /// this value for the token to be accepted.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;
}
