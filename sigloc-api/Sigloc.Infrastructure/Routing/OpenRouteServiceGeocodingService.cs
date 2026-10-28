using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;

namespace Sigloc.Infrastructure.Routing;

/// <summary>
/// <see cref="IRouteGeocodingService"/> backed by OpenRouteService. Geocodes addresses
/// through the Pelias search endpoint and computes distance/duration through the
/// driving-hgv matrix endpoint. All calls are asynchronous.
/// </summary>
public class OpenRouteServiceGeocodingService : IRouteGeocodingService
{
    private const string GeocodePath = "/pelias/v1/search";
    private const string MatrixPath = "/openrouteservice/v2/matrix/driving-hgv";
    private const double MetersPerKilometer = 1000d;
    private const double SecondsPerHour = 3600d;

    // Pelias confidence (0–1) from which a result is accepted without further checks.
    private const double MinimumConfidence = 0.8;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public OpenRouteServiceGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RouteGeometry> ResolveAsync(
        string originAddress,
        string destinationAddress,
        CancellationToken cancellationToken = default)
    {
        var originGeocodeTask = GeocodeAsync(originAddress, cancellationToken);
        var destinationGeocodeTask = GeocodeAsync(destinationAddress, cancellationToken);

        await Task.WhenAll(originGeocodeTask, destinationGeocodeTask);

        var origin = await originGeocodeTask;
        var destination = await destinationGeocodeTask;

        var matrix = await ComputeMatrixAsync(origin.Coordinate, destination.Coordinate, cancellationToken);

        return new RouteGeometry(
            OriginCoordinate: FormatCoordinate(origin.Coordinate),
            DestinationCoordinate: FormatCoordinate(destination.Coordinate),
            DistanceKm: Math.Round(matrix.DistanceMeters / MetersPerKilometer, 2),
            EstimatedTimeHours: Math.Round(matrix.DurationSeconds / SecondsPerHour, 2),
            OriginPlace: origin.Place,
            DestinationPlace: destination.Place);
    }

    public async Task<RouteLeg> ComputeLegAsync(
        string fromCoordinate,
        string toCoordinate,
        CancellationToken cancellationToken = default)
    {
        var from = ParseCoordinate(fromCoordinate);
        var to = ParseCoordinate(toCoordinate);

        var matrix = await ComputeMatrixAsync(from, to, cancellationToken);

        return new RouteLeg(
            DistanceKm: Math.Round(matrix.DistanceMeters / MetersPerKilometer, 2),
            EstimatedTimeHours: Math.Round(matrix.DurationSeconds / SecondsPerHour, 2));
    }

    private static double[] ParseCoordinate(string coordinate)
    {
        var parts = coordinate?.Split(',');
        if (parts is not { Length: 2 } ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
        {
            throw new GeocodingException($"A coordenada '{coordinate}' não está no formato 'longitude,latitude'.");
        }

        // OpenRouteService always works in [longitude, latitude] order.
        return new[] { longitude, latitude };
    }

    private async Task<(double[] Coordinate, string? Place)> GeocodeAsync(string address, CancellationToken cancellationToken)
    {
        var url = $"{GeocodePath}?text={Uri.EscapeDataString(address)}";

        GeocodeResponse? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<GeocodeResponse>(url, SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            throw new GeocodingException($"Não foi possível localizar o endereço '{address}'.", ex);
        }

        var feature = response?.Features?.FirstOrDefault();
        var coordinates = feature?.Geometry?.Coordinates;
        if (coordinates is not { Length: >= 2 } || !IsPlausibleMatch(address, feature!.Properties))
        {
            throw new AddressNotFoundException(address);
        }

        // OpenRouteService always works in [longitude, latitude] order.
        return (new[] { coordinates[0], coordinates[1] }, FormatPlace(feature!.Properties));
    }

    /// <summary>
    /// Pelias always returns its best guess, even for gibberish ("xyzqwv lugar inexistente" came
    /// back as São Paulo). A guess is kept when Pelias is confident in it, or when the city, county
    /// or state it matched was actually typed. Results without a confidence score are kept.
    /// </summary>
    private static bool IsPlausibleMatch(string address, GeocodeProperties? properties)
    {
        if (properties?.Confidence is not double confidence || confidence >= MinimumConfidence)
        {
            return true;
        }

        var typed = $" {Normalize(address)} ";
        return new[] { properties.Locality, properties.LocalAdmin, properties.County, properties.Region }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Any(name => typed.Contains($" {Normalize(name!)} ", StringComparison.Ordinal));
    }

    // Lower case, no accents, punctuation as spaces: "São Paulo, SP" -> "sao paulo sp".
    private static string Normalize(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var c in text.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// "City, UF" from the Pelias result, so a street address still shows its city. Null when the
    /// result has no city-level field (e.g. a whole state was matched).
    /// </summary>
    private static string? FormatPlace(GeocodeProperties? properties)
    {
        var city = FirstText(properties?.Locality, properties?.LocalAdmin, properties?.County);
        if (city is null) return null;
        var state = FirstText(properties?.RegionCode)?.ToUpperInvariant() ?? FirstText(properties?.Region);
        return state is null ? city : $"{city}, {state}";
    }

    private static string? FirstText(params string?[] values) =>
        values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));

    private async Task<(double DistanceMeters, double DurationSeconds)> ComputeMatrixAsync(
        double[] origin,
        double[] destination,
        CancellationToken cancellationToken)
    {
        var request = new MatrixRequest
        {
            Locations = new[] { origin, destination },
            Metrics = new[] { "distance", "duration" }
        };

        MatrixResponse? response;
        try
        {
            using var httpResponse = await _httpClient.PostAsJsonAsync(MatrixPath, request, SerializerOptions, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();
            response = await httpResponse.Content.ReadFromJsonAsync<MatrixResponse>(SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            throw new GeocodingException("Não foi possível calcular a distância e a duração da rota.", ex);
        }

        // The outbound leg (origin → destination) is at [0][1] of each matrix.
        var distance = ReadOutboundValue(response?.Distances, "distance");
        var duration = ReadOutboundValue(response?.Durations, "duration");

        return (distance, duration);
    }

    private static double ReadOutboundValue(double[][]? matrix, string metric)
    {
        if (matrix is not { Length: >= 1 } || matrix[0] is not { Length: >= 2 })
        {
            throw new GeocodingException($"O serviço de rotas retornou uma matriz de {metric} inválida.");
        }

        return matrix[0][1];
    }

    private static string FormatCoordinate(double[] coordinate)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{coordinate[0]},{coordinate[1]}");

    private sealed class GeocodeResponse
    {
        [JsonPropertyName("features")]
        public List<GeocodeFeature>? Features { get; init; }
    }

    private sealed class GeocodeFeature
    {
        [JsonPropertyName("geometry")]
        public GeocodeGeometry? Geometry { get; init; }

        [JsonPropertyName("properties")]
        public GeocodeProperties? Properties { get; init; }
    }

    private sealed class GeocodeProperties
    {
        [JsonPropertyName("locality")]
        public string? Locality { get; init; }

        [JsonPropertyName("localadmin")]
        public string? LocalAdmin { get; init; }

        [JsonPropertyName("county")]
        public string? County { get; init; }

        /// <summary>State abbreviation, e.g. "PR".</summary>
        [JsonPropertyName("region_a")]
        public string? RegionCode { get; init; }

        [JsonPropertyName("region")]
        public string? Region { get; init; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; init; }
    }

    private sealed class GeocodeGeometry
    {
        [JsonPropertyName("coordinates")]
        public double[]? Coordinates { get; init; }
    }

    private sealed class MatrixRequest
    {
        [JsonPropertyName("locations")]
        public required double[][] Locations { get; init; }

        [JsonPropertyName("metrics")]
        public required string[] Metrics { get; init; }
    }

    private sealed class MatrixResponse
    {
        [JsonPropertyName("distances")]
        public double[][]? Distances { get; init; }

        [JsonPropertyName("durations")]
        public double[][]? Durations { get; init; }
    }
}
