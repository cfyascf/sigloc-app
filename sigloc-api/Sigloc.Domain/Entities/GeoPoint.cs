using System.Globalization;
using System.Text.RegularExpressions;

namespace Sigloc.Domain.Entities;

/// <summary>
/// A point given by coordinates instead of an address (farms, yards and quarries often have none).
/// A segment stores it as its address text in a fixed form, "-25.428400, -49.273300" (latitude
/// first, as Google Maps copies it), which is how the geocoder and the forms recognise it.
/// </summary>
public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    // Mainland Brazil plus its oceanic islands (Fernando de Noronha, Trindade), with a small margin.
    private const double MinLatitude = -34.0, MaxLatitude = 5.5, MinLongitude = -74.1, MaxLongitude = -28.5;

    private static readonly Regex TextForm = new(
        @"^\s*(-?\d{1,3}(?:\.\d+)?)\s*,\s*(-?\d{1,3}(?:\.\d+)?)\s*$", RegexOptions.CultureInvariant);

    public bool IsValid => Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;

    public bool IsInBrazil =>
        Latitude is >= MinLatitude and <= MaxLatitude && Longitude is >= MinLongitude and <= MaxLongitude;

    /// <summary>"-25.428400, -49.273300"</summary>
    public string ToText() => string.Create(CultureInfo.InvariantCulture, $"{Latitude:F6}, {Longitude:F6}");

    /// <summary>Reads the text form ("latitude, longitude", dot decimals); false for any other text.</summary>
    public static bool TryParse(string? text, out GeoPoint point)
    {
        point = default;
        var match = text is null ? null : TextForm.Match(text);
        if (match is not { Success: true })
        {
            return false;
        }

        point = new GeoPoint(
            double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>
    /// Why the point can't be used, or null when it can. Points outside Brazil are rejected; a
    /// point that would be in Brazil with its values swapped gets a message saying so.
    /// </summary>
    public string? Problem()
    {
        if (!IsValid)
        {
            return "Coordenada inválida: a latitude vai de -90 a 90 e a longitude de -180 a 180.";
        }

        if (IsInBrazil)
        {
            return null;
        }

        return new GeoPoint(Longitude, Latitude).IsInBrazil
            ? "Latitude e longitude parecem invertidas: no Brasil a latitude vai de -34 a 5 e a longitude de -74 a -29."
            : "A coordenada fica fora do Brasil. No Brasil a latitude vai de -34 a 5 e a longitude de -74 a -29.";
    }
}
