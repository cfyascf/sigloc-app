using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Infrastructure.Monitoring;
using static Sigloc.Tests.Monitoring.ProviderTestSupport;

namespace Sigloc.Tests.Monitoring;

public sealed class MockProviderAndCoordinateTests
{
    [Fact]
    public async Task MockTrackingReturnsConfiguredFreshFixAndDeterministicObservationIdentity()
    {
        var provider = new MockTripTrackingProvider(Options.Create(new MonitoringProviderSettings
        {
            MockLatitude = 12.25, MockLongitude = -42.5
        }));
        var first = await provider.GetLatestAsync(7, Now);
        Assert.Equal(7, first.DeviceId);
        Assert.Equal(12.25, first.Latitude);
        Assert.Equal(-42.5, first.Longitude);
        Assert.Equal(Now, first.FixTime);
        Assert.Equal(first, await provider.GetLatestAsync(7, Now));
        Assert.NotEqual(first.ObservationId, (await provider.GetLatestAsync(7, Now.AddSeconds(1))).ObservationId);
        Assert.NotEqual(first.ObservationId, (await provider.GetLatestAsync(8, Now)).ObservationId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task MockTrackingRejectsNonPositiveDevice(long id)
    {
        var provider = new MockTripTrackingProvider(Options.Create(Settings()));
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => provider.GetLatestAsync(id, Now));
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(0, double.NegativeInfinity)]
    public async Task MockTrackingRejectsInvalidCoordinates(double latitude, double longitude)
    {
        var provider = new MockTripTrackingProvider(Options.Create(new MonitoringProviderSettings
        {
            MockLatitude = latitude, MockLongitude = longitude
        }));
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => provider.GetLatestAsync(7, Now));
    }

    [Fact]
    public async Task MockRoutingSumsHaversineLegsAndComputesFirstLegDurationFromConfiguredSpeed()
    {
        var provider = new MockTripRoutingProvider(Options.Create(new MonitoringProviderSettings { MockSpeedKmPerHour = 60 }));
        var result = await provider.CalculateAsync([new(0, 0), new(0, 1), new(0, 3)]);
        Assert.Equal(333.5852407005987, result.RemainingDistanceKm, 8);
        Assert.Equal(6671.704814011975, result.NextStopDurationSeconds, 8);
    }

    [Fact]
    public async Task MockRoutingCoLocatedFirstStopHasZeroDurationButKeepsLaterDistance()
    {
        var provider = new MockTripRoutingProvider(Options.Create(Settings()));
        var result = await provider.CalculateAsync([new(0, 0), new(0, 0), new(0, 1)]);
        Assert.Equal(111.1950802335329, result.RemainingDistanceKm, 8);
        Assert.Equal(0, result.NextStopDurationSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task MockRoutingEmptySingleAndCoLocatedRoutesReturnZero(int count)
    {
        var provider = new MockTripRoutingProvider(Options.Create(Settings()));
        Assert.Equal(new TripRouteEstimate(0, 0), await provider.CalculateAsync(Enumerable.Repeat(new RouteCoordinate(0, 0), count).ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.Epsilon)]
    public async Task MockRoutingRejectsInvalidSpeedOrNonFiniteResult(double speed)
    {
        var provider = new MockTripRoutingProvider(Options.Create(new MonitoringProviderSettings { MockSpeedKmPerHour = speed }));
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => provider.CalculateAsync(Coordinates(2)));
    }

    [Theory]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    public async Task BothRoutingProvidersAcceptCoordinateBoundaries(double latitude, double longitude)
    {
        var coordinates = new[] { new RouteCoordinate(latitude, longitude), new RouteCoordinate(0, 0) };
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Matrix(2))));
        using var client = new HttpClient(handler);
        await Routing(client).CalculateAsync(coordinates);
        var result = await new MockTripRoutingProvider(Options.Create(Settings())).CalculateAsync(coordinates);
        Assert.True(double.IsFinite(result.RemainingDistanceKm));
    }

    public static TheoryData<RouteCoordinate[]> InvalidCoordinates => new()
    {
        null!,
        new RouteCoordinate[] { null! },
        new RouteCoordinate[] { new(91, 0) },
        new RouteCoordinate[] { new(-91, 0) },
        new RouteCoordinate[] { new(0, 181) },
        new RouteCoordinate[] { new(0, -181) },
        new RouteCoordinate[] { new(double.NaN, 0) },
        new RouteCoordinate[] { new(0, double.NaN) },
        new RouteCoordinate[] { new(double.PositiveInfinity, 0) },
        new RouteCoordinate[] { new(0, double.NegativeInfinity) },
        new RouteCoordinate[] { new(0, 0), new(double.NaN, 0) }
    };

    [Theory]
    [MemberData(nameof(InvalidCoordinates))]
    public async Task BothRoutingProvidersRejectInvalidInputBeforeHttpEvenForSinglePoint(RouteCoordinate[] coordinates)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client).CalculateAsync(coordinates));
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => new MockTripRoutingProvider(Options.Create(Settings())).CalculateAsync(coordinates));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task MockProvidersHonorPreCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new MockTripTrackingProvider(Options.Create(Settings()))
            .GetLatestAsync(7, Now, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new MockTripRoutingProvider(Options.Create(Settings()))
            .CalculateAsync(Coordinates(2), cancellation.Token));
    }

    [Fact]
    public void OperationalDefaultsRemainExplicitAndMocksOptIn()
    {
        var settings = new MonitoringProviderSettings();
        Assert.False(settings.MockMode);
        Assert.Equal(200, settings.GeofenceRadiusMeters);
        Assert.Equal(30, settings.DockBufferMinutes);
        Assert.Equal(15, settings.MaxGpsAgeMinutes);
        Assert.Equal(15, settings.ProviderTimeoutSeconds);
        Assert.Equal(50, settings.RoutingChunkSize);
    }
}
