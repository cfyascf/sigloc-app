using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Infrastructure.Routing;

namespace Sigloc.Infrastructure.Monitoring;

public sealed class OpenRouteServiceTripRoutingProvider(
    HttpClient httpClient,
    IOptions<OpenRouteServiceSettings> routeOptions,
    IOptions<MonitoringProviderSettings> monitoringOptions) : ITripRoutingProvider
{
    private readonly OpenRouteServiceSettings _routeSettings = routeOptions.Value;
    private readonly MonitoringProviderSettings _settings = monitoringOptions.Value;

    public async Task<TripRouteEstimate> CalculateAsync(IReadOnlyList<RouteCoordinate> coordinates, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProviderValidation.ValidateCoordinates(coordinates);
        if (coordinates.Count < 2 || coordinates.All(c => c == coordinates[0]))
            return new TripRouteEstimate(0, 0);
        if (_settings.RoutingChunkSize is < 2 or > 50 || string.IsNullOrWhiteSpace(_routeSettings.ApiKey))
            throw new TrackingUnavailableException();

        using var timeout = ProviderValidation.Timeout(_settings, cancellationToken);
        try
        {
            var endpoint = ProviderValidation.Endpoint(_routeSettings.BaseUrl, "/openrouteservice/v2/matrix/driving-hgv");
            double totalMeters = 0;
            double nextStopSeconds = 0;
            for (var start = 0; start < coordinates.Count - 1; start += _settings.RoutingChunkSize - 1)
            {
                timeout.Token.ThrowIfCancellationRequested();
                var chunk = coordinates.Skip(start).Take(_settings.RoutingChunkSize).ToArray();
                if (chunk.All(c => c == chunk[0]))
                    continue;

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", _routeSettings.ApiKey);
                request.Content = JsonContent.Create(new
                {
                    locations = chunk.Select(c => new[] { c.Longitude, c.Latitude }).ToArray(),
                    metrics = new[] { "distance", "duration" },
                    units = "m"
                });
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();
                var matrix = await response.Content.ReadFromJsonAsync<MatrixResponse>(timeout.Token);
                ValidateDimensions(matrix?.Distances, chunk.Length);
                ValidateDimensions(matrix?.Durations, chunk.Length);
                for (var index = 0; index < chunk.Length - 1; index++)
                {
                    // Only directed consecutive legs belong to this trip, not reverse or cross-stop cells.
                    var coLocated = chunk[index] == chunk[index + 1];
                    var distance = coLocated ? 0 : ReadLeg(matrix!.Distances!, index);
                    var duration = coLocated ? 0 : ReadLeg(matrix!.Durations!, index);
                    totalMeters += distance;
                    if (start == 0 && index == 0)
                        nextStopSeconds = duration;
                }
                if (!double.IsFinite(totalMeters))
                    throw new TrackingUnavailableException();
            }
            timeout.Token.ThrowIfCancellationRequested();
            return new TripRouteEstimate(totalMeters / 1000, nextStopSeconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TrackingUnavailableException();
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException or FormatException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TrackingUnavailableException();
        }
    }

    private static void ValidateDimensions(double?[]?[]? matrix, int size)
    {
        if (matrix is null || matrix.Length != size || matrix.Any(row => row is null || row.Length != size))
            throw new TrackingUnavailableException();
    }

    private static double ReadLeg(double?[]?[] matrix, int index)
    {
        var value = matrix[index]![index + 1];
        if (value is null || !double.IsFinite(value.Value) || value.Value < 0)
            throw new TrackingUnavailableException();
        return value.Value;
    }

    private sealed class MatrixResponse
    {
        public double?[]?[]? Distances { get; init; }
        public double?[]?[]? Durations { get; init; }
    }
}
