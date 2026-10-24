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
    public async Task A_result_without_a_city_leaves_the_place_empty()
    {
        var service = Service(_ => """{"features":[{"geometry":{"coordinates":[-51.0,-24.0]},"properties":{"region":"Paraná","region_a":"PR"}}]}""");

        var geometry = await service.ResolveAsync("Paraná", "Paraná");

        Assert.Null(geometry.OriginPlace);
        Assert.Null(geometry.DestinationPlace);
    }
}
