namespace Sigloc.Application.Services;

/// <summary>
/// Formats the consolidated itinerary strings shown in the auction listing from the
/// ordered list of "City, UF" travel-plan stops.
/// </summary>
internal static class ItineraryFormatter
{
    private const string Arrow = " \u2192 "; // " → "

    /// <summary>Joins the distinct cities (without UF): e.g. "Curitiba → São Paulo → Salvador".</summary>
    public static string Summary(IReadOnlyList<string> orderedCities)
        => string.Join(Arrow, orderedCities.Select(CityOnly));

    /// <summary>Joins the distinct cities keeping the UF: e.g. "Curitiba, PR → São Paulo, SP".</summary>
    public static string WithStates(IReadOnlyList<string> orderedCities)
        => string.Join(Arrow, orderedCities.Select(c => c.Trim()));

    private static string CityOnly(string cityState)
    {
        var comma = cityState.IndexOf(',');
        return (comma >= 0 ? cityState[..comma] : cityState).Trim();
    }
}
