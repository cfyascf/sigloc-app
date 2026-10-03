using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Infrastructure.Monitoring;
using Sigloc.Infrastructure.Routing;
using static Sigloc.Tests.Monitoring.ProviderTestSupport;

namespace Sigloc.Tests.Monitoring;

public sealed class OpenRouteServiceTripRoutingProviderTests
{
    [Fact]
    public async Task SumsOnlyDirectedConsecutiveLegsAndUsesOnlyFirstLegDuration()
    {
        using var handler = new FakeHttpHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://routing.example.invalid/openrouteservice/v2/matrix/driving-hgv", request.RequestUri!.AbsoluteUri);
            Assert.Equal(Secret, Assert.Single(request.Headers.GetValues("Authorization")));
            Assert.DoesNotContain(Secret, request.RequestUri.AbsoluteUri);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal("m", body.RootElement.GetProperty("units").GetString());
            Assert.Equal(new[] { "distance", "duration" }, body.RootElement.GetProperty("metrics").EnumerateArray().Select(c => c.GetString()));
            var locations = body.RootElement.GetProperty("locations");
            Assert.Equal(3, locations.GetArrayLength());
            Assert.Equal(new[] { -46.0, -23.0 }, locations[0].EnumerateArray().Select(c => c.GetDouble()));
            Assert.Equal(new[] { -45.0, -22.0 }, locations[1].EnumerateArray().Select(c => c.GetDouble()));
            return Json(Matrix(3, (i, j) => j == i + 1 ? (i + 1) * 1000 : null,
                (i, j) => j == i + 1 ? (i + 1) * 60 : null));
        });
        using var client = new HttpClient(handler);

        var result = await Routing(client).CalculateAsync([
            new(-23, -46), new(-22, -45), new(-21, -44)]);

        Assert.Equal(3, result.RemainingDistanceKm);
        Assert.Equal(60, result.NextStopDurationSeconds);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(50, 50, 1)]
    [InlineData(51, 50, 2)]
    [InlineData(99, 50, 2)]
    [InlineData(100, 50, 3)]
    [InlineData(6, 3, 3)]
    [InlineData(5, 2, 4)]
    public async Task SplitsAtConfiguredLimitWithOneOverlappingPointAndCountsEveryLegOnce(int count, int limit, int expectedCalls)
    {
        var coordinates = Coordinates(count);
        var chunks = new List<RouteCoordinate[]>();
        using var handler = new FakeHttpHandler(async (request, token) =>
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var chunk = body.RootElement.GetProperty("locations").EnumerateArray()
                .Select(c => new RouteCoordinate(c[1].GetDouble(), c[0].GetDouble())).ToArray();
            Assert.InRange(chunk.Length, 2, limit);
            chunks.Add(chunk);
            return Json(Matrix(chunk.Length));
        });
        using var client = new HttpClient(handler);
        var result = await Routing(client, new() { RoutingChunkSize = limit }).CalculateAsync(coordinates);

        Assert.Equal(count - 1, result.RemainingDistanceKm);
        Assert.Equal(60, result.NextStopDurationSeconds);
        Assert.Equal(expectedCalls, handler.Calls);
        var reconstructed = chunks[0].Concat(chunks.Skip(1).SelectMany(c => c.Skip(1)));
        Assert.Equal(coordinates, reconstructed);
        for (var index = 1; index < chunks.Count; index++)
            Assert.Equal(chunks[index - 1][^1], chunks[index][0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(51)]
    public async Task EmptySingleOrEntirelyCoLocatedRouteReturnsZeroWithoutHttp(int count)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        var coordinates = Enumerable.Repeat(new RouteCoordinate(0, 0), count).ToArray();
        Assert.Equal(new TripRouteEstimate(0, 0), await Routing(client).CalculateAsync(coordinates));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task CoLocatedLegIgnoresUnreachableCellAndHasZeroNextStopDuration()
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Matrix(3,
            (i, j) => i == 1 && j == 2 ? 2500 : null,
            (i, j) => i == 1 && j == 2 ? 300 : null))));
        using var client = new HttpClient(handler);
        Assert.Equal(new TripRouteEstimate(2.5, 0), await Routing(client).CalculateAsync([
            new(0, 0), new(0, 0), new(1, 1)]));
    }

    [Fact]
    public async Task EntireCoLocatedChunkDoesNotRequestHttpOrReplaceFirstLegDuration()
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Matrix(2))));
        using var client = new HttpClient(handler);
        var result = await Routing(client, new() { RoutingChunkSize = 2 }).CalculateAsync([
            new(0, 0), new(0, 0), new(1, 1), new(1, 1), new(2, 2)]);
        Assert.Equal(new TripRouteEstimate(2, 0), result);
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"distances\":[],\"durations\":[]}")]
    [InlineData("{\"distances\":[[0,1],null],\"durations\":[[0,1],[1,0]]}")]
    [InlineData("{\"distances\":[[0,1],[1]],\"durations\":[[0,1],[1,0]]}")]
    [InlineData("{\"distances\":[[0,1],[1,0]],\"durations\":[[0,1]]}")]
    [InlineData("{\"distances\":[[0,1],[1,0]],\"durations\":null}")]
    [InlineData("{\"distances\":[[0,1,2],[1,0,2],[1,2,0]],\"durations\":[[0,1],[1,0]]}")]
    public async Task RejectsMalformedOrIncorrectlySizedMatrices(string body)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(body)));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client).CalculateAsync(Coordinates(2)));
    }

    [Theory]
    [InlineData("distances", "null")]
    [InlineData("distances", "-1")]
    [InlineData("distances", "1e400")]
    [InlineData("distances", "\"NaN\"")]
    [InlineData("distances", "\"Infinity\"")]
    [InlineData("durations", "null")]
    [InlineData("durations", "-1")]
    [InlineData("durations", "1e400")]
    [InlineData("durations", "\"NaN\"")]
    [InlineData("durations", "\"-Infinity\"")]
    public async Task RejectsInvalidDirectedLegDistanceOrDuration(string metric, string value)
    {
        var matrix = JsonNode.Parse(Matrix(3))!;
        matrix[metric]![1]![2] = JsonNode.Parse(value);
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(matrix.ToJsonString())));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client).CalculateAsync(Coordinates(3)));
    }

    [Fact]
    public async Task RejectsSumOverflow()
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Matrix(3, (_, _) => double.MaxValue))));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client).CalculateAsync(Coordinates(3)));
    }

    [Fact]
    public async Task RejectsLaterChunkFailureInsteadOfReturningPartialEstimate()
    {
        var calls = 0;
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(++calls == 1
            ? Json(Matrix(2)) : Json(Secret, HttpStatusCode.InternalServerError)));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client, new() { RoutingChunkSize = 2 })
            .CalculateAsync(Coordinates(3)));
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(51)]
    [InlineData(int.MaxValue)]
    public async Task RejectsInvalidChunkSizeBeforeHttp(int size)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client, new() { RoutingChunkSize = size })
            .CalculateAsync(Coordinates(2)));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("", Secret)]
    [InlineData("relative", Secret)]
    [InlineData("ftp://routing.example.invalid", Secret)]
    [InlineData("https://user:password@routing.example.invalid", Secret)]
    [InlineData("https://routing.example.invalid", "")]
    [InlineData("https://routing.example.invalid", " ")]
    [InlineData("https://routing.example.invalid", "bad\r\nheader")]
    public async Task RejectsInvalidRoutingConfigurationBeforeHttp(string baseUrl, string apiKey)
    {
        using var handler = new FakeHttpHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client, routing: new() { BaseUrl = baseUrl, ApiKey = apiKey })
            .CalculateAsync(Coordinates(2)));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task HttpFailuresAreSanitizedWithoutMockFallback(HttpStatusCode status)
    {
        using var handler = new FakeHttpHandler((_, _) => Task.FromResult(Json(Secret, status)));
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<TrackingUnavailableException>(() => Routing(client).CalculateAsync(Coordinates(2)));
        Assert.Equal("Trip monitoring is temporarily unavailable.", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(Secret, exception.ToString());
    }
}
