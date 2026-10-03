using System.Net;
using Sigloc.Application.Exceptions;
using Sigloc.Infrastructure.Monitoring;
using static Sigloc.Tests.Monitoring.ProviderTestSupport;

namespace Sigloc.Tests.Monitoring;

public sealed class TraccarTripTrackingProviderTests
{
    [Fact]
    public async Task SelectsLatestValidMatchingPositionAndSendsAuthenticatedDeviceRequest()
    {
        using var handler = new FakeHttpHandler((request, token) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://tracking.example.invalid/api/positions?deviceId=7", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal(Secret, request.Headers.Authorization.Parameter);
            Assert.DoesNotContain(Secret, request.RequestUri.AbsoluteUri);
            Assert.True(token.CanBeCanceled);
            return Task.FromResult(Json("[" + string.Join(',',
                Position(10, time: Now.AddMinutes(-10)),
                Position(12, deviceId: 8),
                Position(13, time: Now.AddSeconds(1)),
                Position(14, field: "valid", rawValue: "false"),
                Position(15, time: Now.AddMinutes(-1)),
                Position(16, time: Now.AddMinutes(-2))) + "]"));
        });
        using var client = new HttpClient(handler);

        var fix = await Tracking(client).GetLatestAsync(7, Now);

        Assert.Equal("15", fix.ObservationId);
        Assert.Equal(7, fix.DeviceId);
        Assert.Equal(-23.55, fix.Latitude);
        Assert.Equal(-46.63, fix.Longitude);
        Assert.Equal(Now.AddMinutes(-1), fix.FixTime);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("latitude", "91")]
    [InlineData("latitude", "-91")]
    [InlineData("latitude", "1e400")]
    [InlineData("latitude", "\"NaN\"")]
    [InlineData("latitude", "null")]
    [InlineData("longitude", "181")]
    [InlineData("longitude", "-181")]
    [InlineData("longitude", "-1e400")]
    [InlineData("longitude", "\"Infinity\"")]
    [InlineData("longitude", "{}")]
    [InlineData("valid", "false")]
    [InlineData("valid", "null")]
    [InlineData("valid", "\"true\"")]
    [InlineData("deviceId", "8")]
    [InlineData("deviceId", "null")]
    [InlineData("deviceId", "7.1")]
    [InlineData("id", "0")]
    [InlineData("id", "-1")]
    [InlineData("id", "null")]
    [InlineData("id", "\"invalid\"")]
    [InlineData("fixTime", "\"not-a-date\"")]
    [InlineData("fixTime", "null")]
    [InlineData("fixTime", "123")]
    [InlineData("fixTime", "\"2026-10-01T12:00:00.001Z\"")]
    [InlineData("fixTime", "\"2026-10-01T11:44:59.999Z\"")]
    public async Task SkipsMalformedOrInvalidPositionWithoutDiscardingUsableFix(string field, string value)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(
            "[null,{},3," + Position(9, field: field, rawValue: value) + "," + Position(2, time: Now.AddMinutes(-2)) + "]")));
        using var client = new HttpClient(handler);
        Assert.Equal("2", (await Tracking(client).GetLatestAsync(7, Now)).ObservationId);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null,{},42]")]
    [InlineData("not-json")]
    [InlineData("[{\"valid\":true}]")]
    public async Task RejectsMissingOrMalformedPositions(string body)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(body)));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Tracking(client).GetLatestAsync(7, Now));
    }

    [Theory]
    [InlineData(-15)]
    [InlineData(0)]
    public async Task AcceptsInclusiveAgeBoundariesAndTimezoneOffsets(int minutes)
    {
        var timestamp = Now.AddMinutes(minutes).ToOffset(TimeSpan.FromHours(3));
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json("[" + Position(time: timestamp) + "]")));
        using var client = new HttpClient(handler);
        Assert.Equal(timestamp, (await Tracking(client).GetLatestAsync(7, Now)).FixTime);
    }

    [Theory]
    [InlineData("latitude", "90")]
    [InlineData("latitude", "-90")]
    [InlineData("longitude", "180")]
    [InlineData("longitude", "-180")]
    public async Task AcceptsCoordinateBoundaries(string field, string value)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json("[" + Position(field: field, rawValue: value) + "]")));
        using var client = new HttpClient(handler);
        Assert.Equal("1", (await Tracking(client).GetLatestAsync(7, Now)).ObservationId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public async Task RejectsNonPositiveDeviceBeforeHttp(long deviceId)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Tracking(client).GetLatestAsync(deviceId, Now));
        Assert.Equal(0, handler.Calls);
    }

    public static TheoryData<MonitoringProviderSettings> InvalidSettings => new()
    {
        new() { TraccarBaseUrl = "https://tracking.example.invalid" },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = " " },
        new() { TraccarBaseUrl = "relative", TraccarToken = Secret },
        new() { TraccarBaseUrl = "file:///positions", TraccarToken = Secret },
        new() { TraccarBaseUrl = "https://user:password@tracking.example.invalid", TraccarToken = Secret },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = "bad\r\nheader" },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret, MaxGpsAgeMinutes = 0 },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret, MaxGpsAgeMinutes = -1 },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret, ProviderTimeoutSeconds = 0 },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret, ProviderTimeoutSeconds = -1 },
        new() { TraccarBaseUrl = "https://tracking.example.invalid", TraccarToken = Secret, ProviderTimeoutSeconds = int.MaxValue }
    };

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public async Task RejectsInvalidConfigurationWithoutHttp(MonitoringProviderSettings settings)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Tracking(client, settings).GetLatestAsync(7, Now));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task HttpFailuresAreSanitizedWithoutMockFallback(HttpStatusCode status)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Secret, status)));
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<TrackingUnavailableException>(() => Tracking(client).GetLatestAsync(7, Now));
        Assert.Equal("Trip monitoring is temporarily unavailable.", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(Secret, exception.ToString());
        Assert.Equal(1, handler.Calls);
    }
}
