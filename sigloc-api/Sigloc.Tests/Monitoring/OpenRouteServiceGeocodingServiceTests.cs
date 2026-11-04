using System.Net;
using System.Text;
using Sigloc.Infrastructure.Routing;

namespace Sigloc.Tests.Monitoring;

public class OpenRouteServiceGeocodingServiceTests
{
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static OpenRouteServiceGeocodingService Service(Func<string, string> geocodeBodyFor)
    {
        var handler = new FakeHttpHandler((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath.Contains("pelias")
            ? Json(geocodeBodyFor(Uri.UnescapeDataString(request.RequestUri.Query)))
            : Json("""{"distances":[[0,12000]],"durations":[[0,1800]]}""")));
        return new OpenRouteServiceGeocodingService(new HttpClient(handler) { BaseAddress = new Uri("https://ors.example.invalid") });
    }

    [Fact]
    public async Task Street_addresses_resolve_to_their_city_and_state()
    {
        var service = Service(query => query.Contains("Terra Roxa")
            ? """{"features":[{"geometry":{"coordinates":[-49.27,-25.43]},"properties":{"name":"Rua Terra Roxa","locality":"Curitiba","region":"Paraná","region_a":"pr"}}]}"""
            : """{"features":[{"geometry":{"coordinates":[-49.30,-25.40]},"properties":{"localadmin":"São José dos Pinhais","region":"Paraná"}}]}""");

        var geometry = await service.ResolveAsync("Rua Terra Roxa, 100", "Rua Lothario Boutin, 50");

        Assert.Equal("Curitiba, PR", geometry.OriginPlace);
        Assert.Equal("São José dos Pinhais, Paraná", geometry.DestinationPlace);
        Assert.Equal("-49.27,-25.43", geometry.OriginCoordinate);
        Assert.Equal(12, geometry.DistanceKm);
    }

    [Fact]
    public async Task A_coordinate_skips_the_address_search_and_looks_up_its_city()
    {
        var paths = new List<string>();
        var handler = new FakeHttpHandler((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            paths.Add(path);
            return Task.FromResult(path.EndsWith("/reverse")
                ? Json("""{"features":[{"geometry":{"coordinates":[-49.27,-25.43]},"properties":{"locality":"Curitiba","region_a":"PR"}}]}""")
                : path.EndsWith("/search")
                    ? Json("""{"features":[{"geometry":{"coordinates":[-46.63,-23.55]},"properties":{"locality":"São Paulo","region_a":"SP"}}]}""")
                    : Json("""{"distances":[[0,400000]],"durations":[[0,21600]]}"""));
        });
        var service = new OpenRouteServiceGeocodingService(new HttpClient(handler) { BaseAddress = new Uri("https://ors.example.invalid") });

        var geometry = await service.ResolveAsync("-25.428400, -49.273300", "São Paulo, SP");

        Assert.Equal("-49.2733,-25.4284", geometry.OriginCoordinate);
        Assert.Equal("Curitiba, PR", geometry.OriginPlace);
        Assert.Equal(1, paths.Count(p => p.EndsWith("/search")));
        Assert.Equal(1, paths.Count(p => p.EndsWith("/reverse")));
    }

    [Fact]
    public async Task A_coordinate_without_a_city_still_resolves()
    {
        var handler = new FakeHttpHandler((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath.Contains("pelias")
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
            : Json("""{"distances":[[0,1000]],"durations":[[0,60]]}""")));
        var service = new OpenRouteServiceGeocodingService(new HttpClient(handler) { BaseAddress = new Uri("https://ors.example.invalid") });

        var geometry = await service.ResolveAsync("-25.428400, -49.273300", "-25.500000, -49.300000");

        Assert.Null(geometry.OriginPlace);
        Assert.Equal(1, geometry.DistanceKm);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{"distances":[[0,null]],"durations":[[0,null]]}""")]
    [InlineData(HttpStatusCode.NotFound, """{"error":{"code":6099,"message":"Unable to compute a distance/duration matrix."}}""")]
    public async Task No_road_route_between_the_points_is_reported_as_such(HttpStatusCode status, string matrixBody)
    {
        var handler = new FakeHttpHandler((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath.Contains("pelias")
            ? Json("""{"features":[]}""")
            : new HttpResponseMessage(status) { Content = new StringContent(matrixBody, Encoding.UTF8, "application/json") }));
        var service = new OpenRouteServiceGeocodingService(new HttpClient(handler) { BaseAddress = new Uri("https://ors.example.invalid") });

        await Assert.ThrowsAsync<Sigloc.Application.Exceptions.RouteNotFoundException>(
            () => service.ResolveAsync("-25.000000, -40.000000", "-25.428400, -49.273300"));
    }

    [Fact]
    public async Task A_result_without_a_city_leaves_the_place_empty()
    {
        var service = Service(_ => """{"features":[{"geometry":{"coordinates":[-51.0,-24.0]},"properties":{"region":"Paraná","region_a":"PR"}}]}""");

        var geometry = await service.ResolveAsync("Paraná", "Paraná");

        Assert.Null(geometry.OriginPlace);
        Assert.Null(geometry.DestinationPlace);
    }

    [Fact]
    public async Task A_low_confidence_guess_in_a_city_that_was_not_typed_is_rejected()
    {
        var service = Service(query => query.Contains("xyzqwv")
            ? """{"features":[{"geometry":{"coordinates":[-46.63,-23.55]},"properties":{"locality":"São Paulo","region":"São Paulo","region_a":"SP","confidence":0.4}}]}"""
            : """{"features":[{"geometry":{"coordinates":[-51.16,-23.31]},"properties":{"locality":"Londrina","region_a":"PR","confidence":1}}]}""");

        await Assert.ThrowsAsync<Sigloc.Application.Exceptions.AddressNotFoundException>(
            () => service.ResolveAsync("xyzqwv lugar inexistente 99999", "Londrina, PR"));
    }

    [Theory]
    [InlineData("Rua Inventada 123, Curitiba, PR", "Curitiba", 0.6)]
    [InlineData("rua x, sao paulo", "São Paulo", 0.5)]
    [InlineData("Rua Terra Roxa", "Curitiba", 0.9)]
    public async Task A_guess_is_kept_when_confident_or_when_its_city_was_typed(string address, string locality, double confidence)
    {
        var score = confidence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var body = "{\"features\":[{\"geometry\":{\"coordinates\":[-49.27,-25.43]},\"properties\":{\"locality\":\""
            + locality + "\",\"region_a\":\"PR\",\"confidence\":" + score + "}}]}";
        var service = Service(query => query.Contains("Londrina")
            ? """{"features":[{"geometry":{"coordinates":[-51.16,-23.31]},"properties":{"locality":"Londrina","region_a":"PR","confidence":1}}]}"""
            : body);

        var geometry = await service.ResolveAsync(address, "Londrina, PR");

        Assert.Equal($"{locality}, PR", geometry.OriginPlace);
    }
}
