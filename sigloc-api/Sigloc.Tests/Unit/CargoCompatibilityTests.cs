using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class CargoCompatibilityTests
{
    private static Product P(
        ProductCategory category = ProductCategory.General,
        TransportEnvironment environment = TransportEnvironment.Dry,
        bool dangerous = false,
        bool fragile = false)
    {
        var product = TestData.Product(category: category);
        product.TransportEnvironment = environment;
        product.Dangerous = dangerous;
        product.Fragile = fragile;
        return product;
    }

    [Fact]
    public void Every_category_is_carried_by_at_least_one_body()
    {
        foreach (var category in Enum.GetValues<ProductCategory>())
        {
            CargoCompatibility.BodiesFor(category).Should().NotBeEmpty(category.ToString());
        }
    }

    [Fact]
    public void Every_body_carries_at_least_one_category()
    {
        var carried = Enum.GetValues<ProductCategory>().SelectMany(CargoCompatibility.BodiesFor).ToHashSet();

        carried.Should().BeEquivalentTo(Enum.GetValues<VehicleBodyType>());
    }

    [Theory]
    [InlineData(ProductCategory.SolidBulk, new[] { VehicleBodyType.Graneleiro, VehicleBodyType.Cacamba })]
    [InlineData(ProductCategory.LiquidBulk, new[] { VehicleBodyType.Tanque })]
    [InlineData(ProductCategory.PressurizedBulk, new[] { VehicleBodyType.Silo })]
    [InlineData(ProductCategory.Containerized, new[] { VehicleBodyType.PortaConteiner })]
    [InlineData(ProductCategory.NeoBulk, new[] { VehicleBodyType.GradeBaixa, VehicleBodyType.Prancha, VehicleBodyType.Cegonheiro })]
    [InlineData(ProductCategory.LiveCargo, new[] { VehicleBodyType.Gaiola })]
    public void Dry_loads_are_carried_by_the_category_bodies(ProductCategory category, VehicleBodyType[] expected)
        => CargoCompatibility.AcceptedBodies(category, TransportEnvironment.Dry, fragile: false).Should().BeEquivalentTo(expected);

    [Fact]
    public void Frozen_general_cargo_only_goes_in_a_refrigerated_box()
        => CargoCompatibility.AcceptedBodies(ProductCategory.General, TransportEnvironment.Frozen, fragile: false)
            .Should().Equal(VehicleBodyType.Frigorifico);

    [Fact]
    public void Chilled_liquids_go_in_an_insulated_tank_but_frozen_liquids_have_no_body()
    {
        CargoCompatibility.AcceptedBodies(ProductCategory.LiquidBulk, TransportEnvironment.Chilled, fragile: false)
            .Should().Equal(VehicleBodyType.Tanque);
        CargoCompatibility.AcceptedBodies(ProductCategory.LiquidBulk, TransportEnvironment.Frozen, fragile: false)
            .Should().BeEmpty();
    }

    [Fact]
    public void Fragile_general_cargo_excludes_open_bodies()
        => CargoCompatibility.AcceptedBodies(ProductCategory.General, TransportEnvironment.Dry, fragile: true)
            .Should().BeEquivalentTo([VehicleBodyType.Bau, VehicleBodyType.Frigorifico, VehicleBodyType.Sider]);

    [Theory]
    [InlineData(ProductCategory.General, new[] { TransportEnvironment.Dry, TransportEnvironment.Chilled, TransportEnvironment.Frozen })]
    [InlineData(ProductCategory.Containerized, new[] { TransportEnvironment.Dry, TransportEnvironment.Chilled, TransportEnvironment.Frozen })]
    [InlineData(ProductCategory.LiquidBulk, new[] { TransportEnvironment.Dry, TransportEnvironment.Chilled })]
    [InlineData(ProductCategory.SolidBulk, new[] { TransportEnvironment.Dry })]
    [InlineData(ProductCategory.LiveCargo, new[] { TransportEnvironment.Dry })]
    public void Environments_follow_the_coldest_body_of_the_category(ProductCategory category, TransportEnvironment[] expected)
        => CargoCompatibility.EnvironmentsFor(category).Should().Equal(expected);

    [Theory]
    [InlineData(VehicleBodyType.Frigorifico, new[] { RefrigerationLevel.Resfriado, RefrigerationLevel.Congelado })]
    [InlineData(VehicleBodyType.Tanque, new[] { RefrigerationLevel.Nenhuma, RefrigerationLevel.Resfriado })]
    [InlineData(VehicleBodyType.PortaConteiner, new[] { RefrigerationLevel.Nenhuma, RefrigerationLevel.Resfriado, RefrigerationLevel.Congelado })]
    [InlineData(VehicleBodyType.Sider, new[] { RefrigerationLevel.Nenhuma })]
    public void Refrigeration_levels_depend_on_the_body(VehicleBodyType body, RefrigerationLevel[] expected)
        => CargoCompatibility.RefrigerationLevelsFor(body).Should().Equal(expected);

    [Fact]
    public void Legacy_refrigeration_is_capped_by_the_body()
    {
        CargoCompatibility.EffectiveRefrigeration(TestData.Vehicle(bodyType: VehicleBodyType.Sider, refrigerationLevel: RefrigerationLevel.Congelado))
            .Should().Be(RefrigerationLevel.Nenhuma);
        CargoCompatibility.EffectiveRefrigeration(TestData.Vehicle(bodyType: VehicleBodyType.Frigorifico, refrigerationLevel: RefrigerationLevel.Congelado))
            .Should().Be(RefrigerationLevel.Congelado);
    }

    [Theory]
    [InlineData(ProductCategory.SolidBulk, true)]
    [InlineData(ProductCategory.LiquidBulk, true)]
    [InlineData(ProductCategory.PressurizedBulk, true)]
    [InlineData(ProductCategory.LiveCargo, true)]
    [InlineData(ProductCategory.General, false)]
    [InlineData(ProductCategory.Containerized, false)]
    [InlineData(ProductCategory.NeoBulk, false)]
    public void Bulk_and_live_cargo_travel_as_a_single_product(ProductCategory category, bool expected)
        => CargoCompatibility.IsSingleProduct(category).Should().Be(expected);

    [Fact]
    public void Describe_lists_the_bodies_in_portuguese()
    {
        CargoCompatibility.Describe([VehicleBodyType.Bau, VehicleBodyType.Sider, VehicleBodyType.Frigorifico])
            .Should().Be("Baú, Sider ou Baú Refrigerado");
        CargoCompatibility.Describe([]).Should().Be("Nenhuma carroceria compatível");
    }

    [Fact]
    public void Consolidated_requirement_intersects_the_bodies_of_every_product()
    {
        var requirement = ConsolidatedVehicleRequirement.From([P(ProductCategory.General), P(ProductCategory.SolidBulk)]);

        requirement.AcceptedBodyTypes.Should().Equal(VehicleBodyType.Graneleiro);
    }

    [Fact]
    public void Consolidated_requirement_escalates_refrigeration_and_flags()
    {
        var requirement = ConsolidatedVehicleRequirement.From(
            [P(dangerous: true), P(environment: TransportEnvironment.Frozen, fragile: true)]);

        requirement.AcceptedBodyTypes.Should().Equal(VehicleBodyType.Frigorifico);
        requirement.MinRefrigerationLevel.Should().Be("Congelado");
        requirement.RequiresMopp.Should().BeTrue();
        requirement.RequiresCargoFixing.Should().BeTrue();
    }

    // --- VehicleCompatibilityEvaluator (internal, visible to tests) -----------------

    private static BidRejectedException? Evaluate(Vehicle vehicle, params Product[] products)
        => VehicleCompatibilityEvaluator.Evaluate(vehicle, ConsolidatedVehicleRequirement.From(products));

    [Fact]
    public void Evaluate_compatible_vehicle_returns_null()
        => Evaluate(TestData.Vehicle(), P()).Should().BeNull();

    [Fact]
    public void Evaluate_body_that_does_not_carry_the_category_is_rejected()
    {
        var result = Evaluate(TestData.Vehicle(bodyType: VehicleBodyType.Bau), P(ProductCategory.LiveCargo));

        result.Should().NotBeNull();
        result!.Code.Should().Be(BidRejectionCode.EquipmentMismatch);
        result.Message.Should().Contain("Baú").And.Contain("Gaiola / Boiadeiro");
    }

    [Fact]
    public void Evaluate_refrigeration_below_the_requirement_is_rejected()
    {
        var result = Evaluate(
            TestData.Vehicle(bodyType: VehicleBodyType.PortaConteiner, refrigerationLevel: RefrigerationLevel.Resfriado),
            P(ProductCategory.Containerized, TransportEnvironment.Frozen));

        result.Should().NotBeNull();
        result!.Message.Should().Contain("refrigeração");
    }

    [Fact]
    public void Evaluate_legacy_non_refrigerated_body_marked_frozen_is_rejected()
    {
        var result = Evaluate(
            TestData.Vehicle(bodyType: VehicleBodyType.Sider, refrigerationLevel: RefrigerationLevel.Congelado),
            P(environment: TransportEnvironment.Frozen));

        result.Should().NotBeNull();
        result!.Code.Should().Be(BidRejectionCode.EquipmentMismatch);
    }

    [Fact]
    public void Evaluate_mopp_required_but_missing_is_rejected()
        => Evaluate(TestData.Vehicle(hasMopp: false), P(dangerous: true))!.Message.Should().Contain("MOPP");

    [Fact]
    public void Evaluate_cargo_securing_required_but_missing_is_rejected()
        => Evaluate(TestData.Vehicle(hasCargoSecuring: false), P(fragile: true))!.Message.Should().Contain("fixação de carga");

    [Fact]
    public void Evaluate_fragile_cargo_on_an_open_body_is_rejected()
        => Evaluate(TestData.Vehicle(bodyType: VehicleBodyType.GradeBaixa), P(fragile: true)).Should().NotBeNull();

    [Fact]
    public void Evaluate_refrigerated_box_carries_frozen_dangerous_fragile_general_cargo()
    {
        var vehicle = TestData.Vehicle(
            bodyType: VehicleBodyType.Frigorifico, refrigerationLevel: RefrigerationLevel.Congelado, hasMopp: true, hasCargoSecuring: true);

        Evaluate(vehicle, P(environment: TransportEnvironment.Frozen, dangerous: true, fragile: true)).Should().BeNull();
    }
}
