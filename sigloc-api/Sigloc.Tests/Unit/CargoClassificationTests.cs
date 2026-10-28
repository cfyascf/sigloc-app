using Sigloc.Domain.Entities;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class CargoClassificationTests
{
    private static Product P(bool dangerous, bool fragile)
    {
        var product = TestData.Product();
        product.Dangerous = dangerous;
        product.Fragile = fragile;
        return product;
    }

    [Theory]
    [InlineData(false, false, CargoClass.Comum)]
    [InlineData(false, true, CargoClass.Fragil)]
    [InlineData(true, false, CargoClass.Perigosa)]
    [InlineData(true, true, CargoClass.Perigosa)]
    public void A_product_has_one_class_and_dangerous_wins(bool dangerous, bool fragile, CargoClass expected)
        => CargoClassification.Of(P(dangerous, fragile)).Should().Be(expected);

    [Fact]
    public void Wire_value_is_the_single_class_or_mista()
    {
        CargoClassification.WireValue([]).Should().Be("COMUM");
        CargoClassification.WireValue([P(false, true), P(false, true)]).Should().Be("FRAGIL");
        CargoClassification.WireValue([P(true, false), P(false, false)]).Should().Be("MISTA");
    }
}
