using Sigloc.Application.Services;
using Sigloc.Domain.Entities;

namespace Sigloc.Tests.Unit;

public class TravelPlanBuilderTests
{
    private static readonly DateTimeOffset Day1 = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private static RouteSegment Segment(
        string origin, string destination, DateTimeOffset pickup, DateTimeOffset delivery) => new()
    {
        Id = Guid.NewGuid(),
        OriginAddress = origin,
        DestinationAddress = destination,
        OriginCoordinate = "-49.0,-25.0",
        DestinationCoordinate = "-46.0,-23.0",
        PickupDeadline = pickup,
        DeliveryDeadline = delivery,
    };

    [Fact]
    public void Build_empty_returns_empty()
        => TravelPlanBuilder.Build(Array.Empty<RouteSegment>()).Should().BeEmpty();

    [Fact]
    public void Build_single_segment_yields_pickup_then_delivery()
    {
        var stops = TravelPlanBuilder.Build(new[]
        {
            Segment("Curitiba, PR", "São Paulo, SP", Day1, Day1.AddDays(1)),
        });

        stops.Should().HaveCount(2);
        stops[0].ActionType.Should().Be("Coleta (1 Trecho)");
        stops[0].CityState.Should().Be("Curitiba, PR");
        stops[1].ActionType.Should().Be("Entrega Final");
        stops.Select(s => s.Order).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public void Build_coalesces_same_city_same_day_same_kind()
    {
        var stops = TravelPlanBuilder.Build(new[]
        {
            Segment("Curitiba, PR", "São Paulo, SP", Day1, Day1.AddDays(2)),
            Segment("Curitiba, PR", "Rio de Janeiro, RJ", Day1.AddHours(1), Day1.AddDays(3)),
        });

        var pickup = stops.Should().Contain(s => s.ActionType.StartsWith("Coleta")).Which;
        pickup.ActionType.Should().Be("Coleta (2 Trechos)");
    }

    [Fact]
    public void Build_labels_latest_delivery_final_and_others_partial()
    {
        var stops = TravelPlanBuilder.Build(new[]
        {
            Segment("Curitiba, PR", "São Paulo, SP", Day1, Day1.AddDays(1)),
            Segment("Curitiba, PR", "Rio de Janeiro, RJ", Day1, Day1.AddDays(5)),
        });

        stops.Should().Contain(s => s.ActionType == "Entrega Parcial");
        stops.Should().Contain(s => s.ActionType == "Entrega Final");
        var final = stops.Single(s => s.ActionType == "Entrega Final");
        final.Deadline.Should().Be(Day1.AddDays(5));
    }

    [Fact]
    public void Build_orders_pickup_before_delivery_on_deadline_tie()
    {
        var stops = TravelPlanBuilder.Build(new[]
        {
            Segment("Curitiba, PR", "São Paulo, SP", Day1, Day1),
        });

        stops[0].ActionType.Should().StartWith("Coleta");
        stops[1].ActionType.Should().Contain("Entrega");
    }

    [Fact]
    public void OrderedCities_dedupes_case_insensitively_by_city()
    {
        var stops = TravelPlanBuilder.Build(new[]
        {
            Segment("Curitiba, PR", "São Paulo, SP", Day1, Day1.AddDays(1)),
            Segment("curitiba, PR", "Salvador, BA", Day1.AddDays(1), Day1.AddDays(2)),
        });

        var cities = TravelPlanBuilder.OrderedCities(stops);

        cities.Select(c => c.Split(',')[0].Trim().ToLowerInvariant())
            .Should().OnlyHaveUniqueItems();
        cities.Should().Contain(c => c.StartsWith("Curitiba") || c.StartsWith("curitiba"));
    }
}
