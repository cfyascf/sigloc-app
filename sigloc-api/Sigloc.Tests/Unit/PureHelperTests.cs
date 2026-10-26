using Sigloc.Application.Services;
using Sigloc.Domain.Enums;

namespace Sigloc.Tests.Unit;

public class CnpjFormatterTests
{
    [Fact]
    public void Format_masks_14_digit_cnpj()
        => CnpjFormatter.Format("12345678000199").Should().Be("12.345.678/0001-99");

    [Fact]
    public void Format_reformats_already_punctuated_input()
        => CnpjFormatter.Format("12.345.678/0001-99").Should().Be("12.345.678/0001-99");

    [Fact]
    public void Format_preserves_leading_zeros()
        => CnpjFormatter.Format("00011222000133").Should().Be("00.011.222/0001-33");

    [Theory]
    [InlineData("123")]
    [InlineData("123456789012345")]
    [InlineData("not-a-cnpj")]
    public void Format_returns_original_when_not_14_digits(string input)
        => CnpjFormatter.Format(input).Should().Be(input);
}

public class ItineraryFormatterTests
{
    [Fact]
    public void Summary_joins_cities_without_uf()
        => ItineraryFormatter.Summary(new[] { "Curitiba, PR", "São Paulo, SP" })
            .Should().Be("Curitiba \u2192 São Paulo");

    [Fact]
    public void Summary_trims_city_without_comma()
        => ItineraryFormatter.Summary(new[] { "  Salvador  " }).Should().Be("Salvador");

    [Fact]
    public void Summary_of_empty_is_empty_string()
        => ItineraryFormatter.Summary(Array.Empty<string>()).Should().BeEmpty();

    [Fact]
    public void WithStates_keeps_uf_and_trims()
        => ItineraryFormatter.WithStates(new[] { " Curitiba, PR ", "São Paulo, SP " })
            .Should().Be("Curitiba, PR \u2192 São Paulo, SP");
}

public class AuctionRiskCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Critical_when_expiry_imminent_and_no_bids()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: Now.AddDays(5),
            expiresAt: Now.AddMinutes(10),
            status: AuctionStatus.Open,
            totalBids: 0,
            now: Now);

        result.Should().Be(AuctionRiskCalculator.Critical);
        // Same code as trip risk and the UI badge map; "CRITICAL" rendered unstyled.
        result.Should().Be("CRITIC");
    }

    [Fact]
    public void Critical_when_pickup_imminent_and_open()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: Now.AddHours(3),
            expiresAt: Now.AddDays(1),
            status: AuctionStatus.Open,
            totalBids: 5,
            now: Now);

        result.Should().Be(AuctionRiskCalculator.Critical);
    }

    [Fact]
    public void Not_critical_when_pickup_imminent_but_not_open()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: Now.AddHours(3),
            expiresAt: Now.AddDays(1),
            status: AuctionStatus.Closed,
            totalBids: 5,
            now: Now);

        result.Should().NotBe(AuctionRiskCalculator.Critical);
    }

    [Fact]
    public void Warning_when_pickup_soon()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: Now.AddHours(10),
            expiresAt: Now.AddDays(1),
            status: AuctionStatus.Open,
            totalBids: 5,
            now: Now);

        result.Should().Be(AuctionRiskCalculator.Warning);
    }

    [Fact]
    public void Warning_when_expiry_soon()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: Now.AddDays(5),
            expiresAt: Now.AddHours(1),
            status: AuctionStatus.Open,
            totalBids: 5,
            now: Now);

        result.Should().Be(AuctionRiskCalculator.Warning);
    }

    [Fact]
    public void Normal_when_everything_is_far_out()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: Now.AddDays(5),
            expiresAt: Now.AddDays(3),
            status: AuctionStatus.Open,
            totalBids: 2,
            now: Now);

        result.Should().Be(AuctionRiskCalculator.Normal);
    }

    [Fact]
    public void Normal_when_pickup_unknown_and_expiry_far()
    {
        var result = AuctionRiskCalculator.Evaluate(
            earliestPickup: null,
            expiresAt: Now.AddDays(3),
            status: AuctionStatus.Open,
            totalBids: 0,
            now: Now);

        result.Should().Be(AuctionRiskCalculator.Normal);
    }
}

public class RouteSegmentRiskCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Critical_when_available_and_pickup_imminent()
    {
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddHours(3),
            status: SegmentStatus.Available,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Critical);
        // Same code as the auction/trip risk and the UI badge map.
        result.Should().Be("CRITIC");
    }

    [Fact]
    public void Critical_when_available_and_pickup_already_past()
    {
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddHours(-2),
            status: SegmentStatus.Available,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Critical);
    }

    [Fact]
    public void Warning_at_the_six_hour_boundary()
    {
        // Exactly 6h out is no longer critical (strict "< 6h") but still within the 12h warning window.
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddHours(6),
            status: SegmentStatus.Available,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Warning);
    }

    [Fact]
    public void Warning_when_available_and_pickup_soon()
    {
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddHours(10),
            status: SegmentStatus.Available,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Warning);
    }

    [Fact]
    public void Normal_at_the_twelve_hour_boundary()
    {
        // Exactly 12h out falls outside the strict "< 12h" warning window.
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddHours(12),
            status: SegmentStatus.Available,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Normal);
    }

    [Fact]
    public void Normal_when_available_and_pickup_far_out()
    {
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddDays(3),
            status: SegmentStatus.Available,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Normal);
    }

    [Theory]
    [InlineData(SegmentStatus.Routed)]
    [InlineData(SegmentStatus.InExecution)]
    [InlineData(SegmentStatus.Completed)]
    public void Normal_when_not_available_even_if_pickup_imminent(SegmentStatus status)
    {
        var result = RouteSegmentRiskCalculator.Evaluate(
            pickupDeadline: Now.AddHours(1),
            status: status,
            now: Now);

        result.Should().Be(RouteSegmentRiskCalculator.Normal);
    }
}

public class BrasiliaTimeTests
{
    [Fact]
    public void Format_shows_utc_deadlines_in_brasilia_time()
        => BrasiliaTime.Format(new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero)).Should().Be("25/10/2026 09:00");
}

public class VehicleBodyTypeDescriptionTests
{
    [Theory]
    [InlineData(VehicleBodyType.GradeBaixa, "Grade Baixa")]
    [InlineData(VehicleBodyType.Frigorifico, "Baú Refrigerado")]
    public void Description_matches_the_body_type(VehicleBodyType bodyType, string expected)
    {
        var description = typeof(VehicleBodyType).GetField(bodyType.ToString())!
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .Cast<System.ComponentModel.DescriptionAttribute>()
            .Single();

        description.Description.Should().Be(expected);
    }
}
