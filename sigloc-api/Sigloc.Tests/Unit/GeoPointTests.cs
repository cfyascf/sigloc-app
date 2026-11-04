using Sigloc.Domain.Entities;

namespace Sigloc.Tests.Unit;

public class GeoPointTests
{
    [Theory]
    [InlineData("-25.4284, -49.2733", -25.4284, -49.2733)]
    [InlineData(" -25.428400,-49.273300 ", -25.4284, -49.2733)]
    [InlineData("-3, -60", -3, -60)]
    public void Text_form_is_latitude_then_longitude(string text, double latitude, double longitude)
    {
        GeoPoint.TryParse(text, out var point).Should().BeTrue();
        point.Should().Be(new GeoPoint(latitude, longitude));
    }

    [Theory]
    [InlineData("Curitiba, PR")]
    [InlineData("Rua 25, 100")]
    [InlineData("-25,4284 -49,2733")]
    [InlineData("")]
    [InlineData(null)]
    public void Addresses_are_not_coordinates(string? text)
        => GeoPoint.TryParse(text, out _).Should().BeFalse();

    [Fact]
    public void Text_form_round_trips_with_six_decimals()
    {
        var text = new GeoPoint(-25.4284, -49.2733).ToText();

        text.Should().Be("-25.428400, -49.273300");
        GeoPoint.TryParse(text, out var point).Should().BeTrue();
        point.Should().Be(new GeoPoint(-25.4284, -49.2733));
    }

    [Theory]
    [InlineData(-25.43, -49.27)]   // Curitiba
    [InlineData(2.82, -60.67)]     // Boa Vista
    [InlineData(-3.85, -32.42)]    // Fernando de Noronha
    public void Points_in_brazil_are_accepted(double latitude, double longitude)
        => new GeoPoint(latitude, longitude).Problem().Should().BeNull();

    [Fact]
    public void Swapped_values_are_reported_as_swapped()
        => new GeoPoint(-49.27, -25.43).Problem().Should().Contain("invertidas");

    [Fact]
    public void Points_outside_brazil_are_rejected()
        => new GeoPoint(40.71, -74.0).Problem().Should().Contain("fora do Brasil");

    [Fact]
    public void Out_of_range_values_are_invalid()
        => new GeoPoint(-125, 30).Problem().Should().Contain("inválida");
}
