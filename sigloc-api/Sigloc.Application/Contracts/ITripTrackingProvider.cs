namespace Sigloc.Application.Contracts;

public record TrackingFix(string ObservationId, long DeviceId, double Latitude, double Longitude, DateTimeOffset FixTime);

public interface ITripTrackingProvider
{
    Task<TrackingFix> GetLatestAsync(long deviceId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
