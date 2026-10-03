using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;

namespace Sigloc.Infrastructure.Monitoring;

public sealed class TraccarTripTrackingProvider(HttpClient httpClient, IOptions<MonitoringProviderSettings> options) : ITripTrackingProvider
{
    private readonly MonitoringProviderSettings _settings = options.Value;

    public async Task<TrackingFix> GetLatestAsync(long deviceId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (deviceId <= 0 || _settings.MaxGpsAgeMinutes <= 0 || string.IsNullOrWhiteSpace(_settings.TraccarToken))
            throw new TrackingUnavailableException();

        using var timeout = ProviderValidation.Timeout(_settings, cancellationToken);
        try
        {
            var path = "/api/positions?deviceId=" + deviceId.ToString(CultureInfo.InvariantCulture);
            using var request = new HttpRequestMessage(HttpMethod.Get, ProviderValidation.Endpoint(_settings.TraccarBaseUrl, path));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.TraccarToken);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            using var positions = await response.Content.ReadFromJsonAsync<JsonDocument>(timeout.Token);
            if (positions is null || positions.RootElement.ValueKind != JsonValueKind.Array)
                throw new TrackingUnavailableException();

            TrackingFix? latest = null;
            foreach (var position in positions.RootElement.EnumerateArray())
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (position.ValueKind != JsonValueKind.Object ||
                    !TryInt64(position, "id", out var id) || id <= 0 ||
                    !TryInt64(position, "deviceId", out var positionDeviceId) || positionDeviceId != deviceId ||
                    !position.TryGetProperty("valid", out var valid) || valid.ValueKind != JsonValueKind.True ||
                    !TryDouble(position, "latitude", out var latitude) ||
                    !TryDouble(position, "longitude", out var longitude) ||
                    !ProviderValidation.IsCoordinateValid(latitude, longitude) ||
                    !position.TryGetProperty("fixTime", out var timestamp) || timestamp.ValueKind != JsonValueKind.String ||
                    !DateTimeOffset.TryParse(timestamp.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var fixTime) ||
                    fixTime > now || (now - fixTime).TotalMinutes > _settings.MaxGpsAgeMinutes)
                    continue;

                if (latest is null || fixTime > latest.FixTime)
                    latest = new TrackingFix(id.ToString(CultureInfo.InvariantCulture), deviceId, latitude, longitude, fixTime);
            }

            timeout.Token.ThrowIfCancellationRequested();
            return latest ?? throw new TrackingUnavailableException();
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

    private static bool TryInt64(JsonElement position, string name, out long value)
    {
        value = 0;
        return position.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out value);
    }

    private static bool TryDouble(JsonElement position, string name, out double value)
    {
        value = 0;
        return position.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out value);
    }
}
