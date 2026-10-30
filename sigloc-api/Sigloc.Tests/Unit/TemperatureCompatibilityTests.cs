using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class TemperatureCompatibilityTests
{
    private static Product Dry() => TestData.Product(name: "Arroz");
    private static Product Chilled(double min = 2, double max = 8) => TestData.Refrigerated(TransportEnvironment.Chilled, min, max, name: "Iogurte");
    private static Product Frozen(double min = -25, double max = -18) => TestData.Refrigerated(TransportEnvironment.Frozen, min, max, name: "Sorvete");

    [Fact]
    public void Dry_products_travel_at_ambient_temperature_whatever_range_they_carry()
    {
        var dry = Dry();
        dry.TempMin = -30;
        dry.TempMax = -20;

        TemperatureCompatibility.RangeOf(dry).Should().Be(new TemperatureRange(0, 35));
    }

    [Fact]
    public void Dry_and_chilled_share_a_temperature()
        => TemperatureCompatibility.FindConflict([Dry(), Chilled()]).Should().BeNull();

    [Fact]
    public void Dry_and_frozen_conflict()
    {
        var dry = Dry();
        var frozen = Frozen();

        TemperatureCompatibility.FindConflict([frozen, dry]).Should().Be((dry, frozen));
    }

    [Fact]
    public void Chilled_and_frozen_conflict()
        => TemperatureCompatibility.FindConflict([Chilled(), Frozen()]).Should().NotBeNull();

    [Theory]
    [InlineData(2, 8, 0, 10)]     // nested
    [InlineData(2, 8, 8, 12)]     // touching at 8 °C
    [InlineData(-25, -18, -20, -15)]
    public void Overlapping_ranges_share_a_temperature(double min1, double max1, double min2, double max2)
        => TemperatureCompatibility.FindConflict([Chilled(min1, max1), Chilled(min2, max2)]).Should().BeNull();

    [Fact]
    public void Every_product_must_fit_one_temperature_not_just_each_pair_in_sequence()
        // 0–10 overlaps both, but 0–2 and 8–10 don't overlap each other.
        => TemperatureCompatibility.FindConflict([Chilled(0, 2), Chilled(0, 10), Chilled(8, 10)]).Should().NotBeNull();

    [Fact]
    public void A_missing_bound_on_older_data_is_left_open()
    {
        var noMax = TestData.Refrigerated(TransportEnvironment.Chilled, 2, null);

        TemperatureCompatibility.FindConflict([Dry(), noMax]).Should().BeNull();
        TemperatureCompatibility.Describe(noMax).Should().Be("Produto Refrigerado (a partir de 2 °C)");
    }

    [Fact]
    public void Describe_shows_the_range_and_marks_dry_products_as_ambient()
    {
        TemperatureCompatibility.Describe(Frozen(-25, -18)).Should().Be("Sorvete (-25 °C a -18 °C)");
        TemperatureCompatibility.Describe(Chilled(2.5, 8)).Should().Be("Iogurte (2,5 °C a 8 °C)");
        TemperatureCompatibility.Describe(Dry()).Should().Be("Arroz (ambiente, 0 °C a 35 °C)");
    }
}
