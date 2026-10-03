using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Infrastructure.Monitoring;
using Sigloc.Infrastructure.Routing;

namespace Sigloc.Tests.Monitoring;

internal sealed class FakeHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return send(request, cancellationToken);
    }
}

internal static class ProviderTestSupport
{
    internal static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    internal const string Secret = "test-only-provider-secret";

    internal static MonitoringProviderSettings Settings() => new()
    {
        TraccarBaseUrl = "https://tracking.example.invalid",
        TraccarToken = Secret
    };

    internal static TraccarTripTrackingProvider Tracking(HttpClient client, MonitoringProviderSettings? settings = null) =>
        new(client, Options.Create(settings ?? Settings()));

    internal static OpenRouteServiceTripRoutingProvider Routing(HttpClient client,
        MonitoringProviderSettings? settings = null, OpenRouteServiceSettings? routing = null) =>
        new(client, Options.Create(routing ?? new OpenRouteServiceSettings
        {
            BaseUrl = "https://routing.example.invalid", ApiKey = Secret
        }), Options.Create(settings ?? Settings()));

    internal static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    internal static string Position(long id = 1, long deviceId = 7, DateTimeOffset? time = null,
        string? field = null, string? rawValue = null)
    {
        var position = JsonSerializer.SerializeToNode(new
        {
            id, deviceId, valid = true, latitude = -23.55, longitude = -46.63,
            fixTime = (time ?? Now).ToString("O")
        })!;
        if (field is not null)
            position[field] = rawValue is null ? null : JsonNode.Parse(rawValue);
        return position.ToJsonString();
    }

    internal static RouteCoordinate[] Coordinates(int count) =>
        Enumerable.Range(0, count).Select(i => new RouteCoordinate(i % 80, i % 170)).ToArray();

    internal static string Matrix(int size, Func<int, int, double?>? distances = null,
        Func<int, int, double?>? durations = null) => JsonSerializer.Serialize(new
    {
        distances = Enumerable.Range(0, size).Select(from => Enumerable.Range(0, size)
            .Select(to => distances is null ? (double?)1000 : distances(from, to)).ToArray()).ToArray(),
        durations = Enumerable.Range(0, size).Select(from => Enumerable.Range(0, size)
            .Select(to => durations is null ? (double?)60 : durations(from, to)).ToArray()).ToArray()
    });
}
