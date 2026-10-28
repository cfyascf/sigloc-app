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
