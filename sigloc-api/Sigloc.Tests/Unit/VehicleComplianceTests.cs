using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class VehicleComplianceTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);

    /// <summary>A vehicle with every document filled in and in date (single truck, own RNTRC).</summary>
    private static Vehicle Compliant(Action<Vehicle>? change = null, bool hasMopp = false)
    {
        var vehicle = TestData.Vehicle(hasMopp: hasMopp);
        vehicle.Composition = VehicleComposition.Rigid;
        vehicle.RntrcSource = RntrcSource.ThirdParty;
        vehicle.RntrcNumber = "12345678";
        vehicle.RntrcCategory = RntrcCategory.TAC;
        vehicle.RntrcValidUntil = Today.AddYears(1);
        vehicle.DriverCnhCategory = CnhCategory.C;
        vehicle.DriverHasEar = true;
        vehicle.DriverToxicologicalExamValidUntil = Today.AddYears(1);
        change?.Invoke(vehicle);
        return vehicle;
    }

    private static Carrier CarrierWithRntrc(DateOnly? validUntil) => new()
    {
        Cnpj = "11222333000181",
        CompanyName = "Transportadora",
        RntrcNumber = "87654321",
        RntrcCategory = RntrcCategory.ETC,
        RntrcValidUntil = validUntil,
    };

    private static ConsolidatedVehicleRequirement Requirement(bool mopp = false, bool civCipp = false) =>
        new([VehicleBodyType.Bau], "Nenhuma", mopp, false, civCipp);

    [Fact]
    public void A_compliant_vehicle_has_no_issues()
        => VehicleCompliance.Check(Compliant(), null, Today).Should().BeEmpty();

    [Fact]
    public void Missing_documents_only_warn()
    {
        var issues = VehicleCompliance.Check(TestData.Vehicle(), null, Today);

        issues.Should().NotBeEmpty().And.OnlyContain(i => i.Severity == ComplianceSeverity.Warning);
        issues.Select(i => i.Code).Should().Contain(["RNTRC_MISSING", "COMPOSITION_MISSING", "CNH_MISSING"]);
    }

    [Fact]
    public void An_expired_rntrc_blocks_and_one_about_to_expire_warns()
    {
        VehicleCompliance.Check(Compliant(v => v.RntrcValidUntil = Today.AddDays(-1)), null, Today)
            .Should().ContainSingle(i => i.Code == "RNTRC_EXPIRED" && i.Severity == ComplianceSeverity.Blocking);
        VehicleCompliance.Check(Compliant(v => v.RntrcValidUntil = Today.AddDays(10)), null, Today)
            .Should().ContainSingle(i => i.Code == "RNTRC_EXPIRING" && i.Severity == ComplianceSeverity.Warning);
    }

    [Fact]
    public void A_vehicle_under_the_carrier_rntrc_uses_the_carrier_data()
    {
        var vehicle = Compliant(v => { v.RntrcSource = RntrcSource.Carrier; v.RntrcNumber = null; v.RntrcValidUntil = null; });

        VehicleCompliance.Check(vehicle, CarrierWithRntrc(Today.AddYears(1)), Today).Should().BeEmpty();
        VehicleCompliance.Check(vehicle, CarrierWithRntrc(Today.AddDays(-3)), Today)
            .Should().ContainSingle(i => i.Code == "RNTRC_EXPIRED");
        VehicleCompliance.Check(vehicle, null, Today)
            .Should().ContainSingle(i => i.Code == "RNTRC_MISSING" && i.Message.Contains("Perfil"));
    }

    [Theory]
    [InlineData(VehicleComposition.Articulated, CnhCategory.C, true)]
    [InlineData(VehicleComposition.Bitrain, CnhCategory.D, true)]
    [InlineData(VehicleComposition.Rodotrain, CnhCategory.E, false)]
    [InlineData(VehicleComposition.Rigid, CnhCategory.C, false)]
    public void Combinations_need_a_driver_with_cnh_e(VehicleComposition composition, CnhCategory cnh, bool blocked)
    {
        var issues = VehicleCompliance.Check(Compliant(v => { v.Composition = composition; v.DriverCnhCategory = cnh; }), null, Today);

        issues.Any(i => i.Code == "CNH_CATEGORY" && i.Severity == ComplianceSeverity.Blocking).Should().Be(blocked);
    }

    [Fact]
    public void A_cnh_without_ear_or_an_expired_toxicological_exam_blocks()
    {
        VehicleCompliance.Check(Compliant(v => v.DriverHasEar = false), null, Today)
            .Should().ContainSingle(i => i.Code == "EAR_MISSING" && i.Severity == ComplianceSeverity.Blocking);
        VehicleCompliance.Check(Compliant(v => v.DriverToxicologicalExamValidUntil = Today.AddDays(-1)), null, Today)
            .Should().ContainSingle(i => i.Code == "TOXICOLOGICAL_EXPIRED" && i.Severity == ComplianceSeverity.Blocking);
    }

    [Fact]
    public void An_expired_mopp_blocks_only_dangerous_cargo()
    {
        var vehicle = Compliant(v => v.MoppValidUntil = Today.AddDays(-1), hasMopp: true);

        VehicleCompliance.Check(vehicle, null, Today)
            .Should().ContainSingle(i => i.Code == "MOPP_EXPIRED" && i.Severity == ComplianceSeverity.Warning);
        VehicleCompliance.CheckForCargo(vehicle, null, Today, Requirement())
            .Should().NotContain(i => i.Code == "MOPP_EXPIRED");
        VehicleCompliance.CheckForCargo(vehicle, null, Today, Requirement(mopp: true))
            .Should().ContainSingle(i => i.Code == "MOPP_EXPIRED" && i.Severity == ComplianceSeverity.Blocking);
    }

    [Fact]
    public void Expired_civ_cipp_block_only_dangerous_bulk()
    {
        var vehicle = Compliant(v => v.CivCippValidUntil = Today.AddDays(-1));

        VehicleCompliance.CheckForCargo(vehicle, null, Today, Requirement(mopp: true))
            .Should().NotContain(i => i.Code == "CIV_CIPP_EXPIRED");
        VehicleCompliance.CheckForCargo(vehicle, null, Today, Requirement(mopp: true, civCipp: true))
            .Should().ContainSingle(i => i.Code == "CIV_CIPP_EXPIRED" && i.Severity == ComplianceSeverity.Blocking);
    }

    [Fact]
    public void Dangerous_bulk_requires_civ_cipp()
    {
        var acid = TestData.Product(category: ProductCategory.LiquidBulk);
        acid.Dangerous = true;
        var paint = TestData.Product();
        paint.Dangerous = true;

        ConsolidatedVehicleRequirement.From([acid]).RequiresCivCipp.Should().BeTrue();
        ConsolidatedVehicleRequirement.From([paint]).RequiresCivCipp.Should().BeFalse();
    }
}

public class TransportDocumentsTests
{
    [Theory]
    [InlineData("12345678", "12345678")]
    [InlineData("012345678", "12345678")]   // ANTT's lookup shows a leading zero
    [InlineData("1234.5678", "12345678")]
    [InlineData("1234567", null)]
    [InlineData("00000000", null)]
    [InlineData("12345678a", null)]
    [InlineData("", null)]
    public void Rntrc_is_normalised_to_8_digits(string text, string? expected)
        => TransportDocuments.NormalizeRntrc(text).Should().Be(expected);

    [Theory]
    [InlineData("52998224725", true)]
    [InlineData("52998224724", false)]
    [InlineData("11111111111", false)]
    [InlineData("5299822472", false)]
    public void Cpf_check_digits_are_verified(string cpf, bool valid)
        => TransportDocuments.IsValidCpf(cpf).Should().Be(valid);
}
