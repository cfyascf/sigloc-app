namespace Sigloc.Infrastructure.Monitoring;

public sealed class MonitoringProviderSettings
{
    public const string SectionName = "Monitoring";

    public string TraccarBaseUrl { get; init; } = string.Empty;
    public string TraccarToken { get; init; } = string.Empty;
    public bool MockMode { get; init; }
    public int MaxGpsAgeMinutes { get; init; } = 15;
    public double GeofenceRadiusMeters { get; init; } = 200;
    public int DockBufferMinutes { get; init; } = 30;
    public int ProviderTimeoutSeconds { get; init; } = 15;
    public double MockLatitude { get; init; } = -23.5505;
    public double MockLongitude { get; init; } = -46.6333;
    public double MockSpeedKmPerHour { get; init; } = 60;

    /// <summary>Two to fifty locations per matrix; adjacent chunks share one location.</summary>
    public int RoutingChunkSize { get; init; } = 50;
}
