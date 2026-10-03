using System.Globalization;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;

namespace Sigloc.Infrastructure.Monitoring;

public sealed class MockTripTrackingProvider(IOptions<MonitoringProviderSettings> options) : ITripTrackingProvider
{
    private readonly MonitoringProviderSettings _settings = options.Value;

    public Task<TrackingFix> GetLatestAsync(long deviceId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (deviceId <= 0 || !ProviderValidation.IsCoordinateValid(_settings.MockLatitude, _settings.MockLongitude))
            throw new TrackingUnavailableException();

        var observationId = string.Create(CultureInfo.InvariantCulture, $"mock:{deviceId}:{now.UtcTicks}");
        return Task.FromResult(new TrackingFix(observationId, deviceId, _settings.MockLatitude, _settings.MockLongitude, now));
    }
}
