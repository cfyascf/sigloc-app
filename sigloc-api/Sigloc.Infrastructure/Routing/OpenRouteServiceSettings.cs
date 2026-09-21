namespace Sigloc.Infrastructure.Routing;

public class OpenRouteServiceSettings
{
    // Keeps the appsettings.json section name tied to this class.
    public const string SectionName = "OpenRouteService";

    /// <summary>API key sent in the Authorization header on every request.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Base address of the OpenRouteService host.</summary>
    public string BaseUrl { get; init; } = "https://api.heigit.org";
}
