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

        var matrix = await ComputeMatrixAsync(origin, destination, cancellationToken);

        return new RouteGeometry(
            OriginCoordinate: FormatCoordinate(origin),
            DestinationCoordinate: FormatCoordinate(destination),
            DistanceKm: Math.Round(matrix.DistanceMeters / MetersPerKilometer, 2),
            EstimatedTimeHours: Math.Round(matrix.DurationSeconds / SecondsPerHour, 2));
    }

    private async Task<double[]> GeocodeAsync(string address, CancellationToken cancellationToken)
    {
        var url = $"{GeocodePath}?text={Uri.EscapeDataString(address)}";

        GeocodeResponse? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<GeocodeResponse>(url, SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            throw new GeocodingException($"Could not geocode the address '{address}'.", ex);
        }

        var coordinates = response?.Features?.FirstOrDefault()?.Geometry?.Coordinates;
        if (coordinates is not { Length: >= 2 })
        {
            throw new GeocodingException($"No coordinates were found for the address '{address}'.");
        }

        // OpenRouteService always works in [longitude, latitude] order.
        return new[] { coordinates[0], coordinates[1] };
    }

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
            throw new GeocodingException("Could not compute the route distance and duration.", ex);
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
            throw new GeocodingException($"The routing provider returned an invalid {metric} matrix.");
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
