using System.Globalization;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

public enum ComplianceSeverity
{
    /// <summary>Missing or about to expire: shown on the fleet, never blocks a bid.</summary>
    Warning,

    /// <summary>Known to be invalid: the vehicle can't take the freight.</summary>
    Blocking
}

/// <summary>A document problem of a vehicle or its driver. <c>Code</c> is stable for the UI.</summary>
public sealed record ComplianceIssue(string Code, ComplianceSeverity Severity, string Message);

/// <summary>
/// Document rules for running paid freight: the vehicle's RNTRC (ANTT, revalidated yearly), the
/// driver's CNH (category, EAR, toxicological exam) and, for some cargo, MOPP and CIV/CIPP.
/// Missing data only warns, since vehicles saved before these fields existed have none; data
/// that is filled in and invalid blocks.
/// </summary>
public static class VehicleCompliance
{
    /// <summary>Days ahead of a due date from which the fleet warns.</summary>
    public const int ExpiryWarningDays = 30;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>The RNTRC the vehicle actually runs under: the carrier's or the third party's.</summary>
    public static (string? Number, RntrcCategory? Category, DateOnly? ValidUntil) EffectiveRntrc(Vehicle vehicle, Carrier? carrier) =>
        vehicle.RntrcSource == RntrcSource.Carrier
            ? (carrier?.RntrcNumber, carrier?.RntrcCategory, carrier?.RntrcValidUntil)
            : (vehicle.RntrcNumber, vehicle.RntrcCategory, vehicle.RntrcValidUntil);

    /// <summary>Problems of the vehicle and its driver regardless of cargo (shown on the fleet).</summary>
    public static IReadOnlyList<ComplianceIssue> Check(Vehicle vehicle, Carrier? carrier, DateOnly today)
    {
        var issues = new List<ComplianceIssue>();
        CheckRntrc(vehicle, carrier, today, issues);
        CheckDriver(vehicle, today, issues);

        // Only relevant for some cargo, so they warn here and block in CheckForCargo.
        if (vehicle.HasMopp && vehicle.MoppValidUntil is DateOnly mopp && mopp < today)
        {
            issues.Add(new("MOPP_EXPIRED", ComplianceSeverity.Warning,
                $"MOPP do motorista vencido em {Format(mopp)}: o veículo não pode levar carga perigosa."));
        }

        if (vehicle.CivCippValidUntil is DateOnly civ && civ < today)
        {
            issues.Add(new("CIV_CIPP_EXPIRED", ComplianceSeverity.Warning,
                $"CIV/CIPP vencidos em {Format(civ)}: o veículo não pode levar granel perigoso."));
        }

        return issues;
    }

    /// <summary>Problems for carrying this cargo: the vehicle's own plus MOPP and CIV/CIPP when required.</summary>
    public static IReadOnlyList<ComplianceIssue> CheckForCargo(
        Vehicle vehicle, Carrier? carrier, DateOnly today, ConsolidatedVehicleRequirement requirement)
    {
        var issues = Check(vehicle, carrier, today)
            .Where(i => i.Code is not ("MOPP_EXPIRED" or "CIV_CIPP_EXPIRED"))
            .ToList();

        if (requirement.RequiresMopp && vehicle.MoppValidUntil is DateOnly mopp && mopp < today)
        {
            issues.Add(new("MOPP_EXPIRED", ComplianceSeverity.Blocking,
                $"MOPP do motorista vencido em {Format(mopp)} (carga perigosa)."));
        }

        if (requirement.RequiresCivCipp && vehicle.CivCippValidUntil is DateOnly civ && civ < today)
        {
            issues.Add(new("CIV_CIPP_EXPIRED", ComplianceSeverity.Blocking,
                $"CIV/CIPP vencidos em {Format(civ)} (granel perigoso exige inspeção do Inmetro em dia)."));
        }

        return issues;
    }

    private static void CheckRntrc(Vehicle vehicle, Carrier? carrier, DateOnly today, List<ComplianceIssue> issues)
    {
        if (vehicle.RntrcSource is null)
        {
            issues.Add(new("RNTRC_MISSING", ComplianceSeverity.Warning, "RNTRC (ANTT) do veículo não informado."));
            return;
        }

        var (number, _, validUntil) = EffectiveRntrc(vehicle, carrier);
        if (string.IsNullOrEmpty(number))
        {
            issues.Add(new("RNTRC_MISSING", ComplianceSeverity.Warning, vehicle.RntrcSource == RntrcSource.Carrier
                ? "O veículo usa o RNTRC da transportadora, que ainda não foi cadastrado no Perfil."
                : "RNTRC (ANTT) do veículo não informado."));
            return;
        }

        AddExpiry(issues, "RNTRC", validUntil, today, "Validade do RNTRC não informada.",
            date => $"RNTRC vencido em {date}: a revalidação na ANTT é anual.",
            date => $"RNTRC vence em {date}: revalide na ANTT.");
    }

    private static void CheckDriver(Vehicle vehicle, DateOnly today, List<ComplianceIssue> issues)
    {
        if (vehicle.Composition is null)
        {
            issues.Add(new("COMPOSITION_MISSING", ComplianceSeverity.Warning, "Composição do veículo não informada."));
        }

        if (string.IsNullOrWhiteSpace(vehicle.Driver))
        {
            issues.Add(new("DRIVER_MISSING", ComplianceSeverity.Warning, "Nenhum motorista atribuído ao veículo."));
            return;
        }

        if (vehicle.DriverCnhCategory is not CnhCategory cnh)
        {
            issues.Add(new("CNH_MISSING", ComplianceSeverity.Warning, "Categoria da CNH do motorista não informada."));
        }
        else if (vehicle.Composition is VehicleComposition composition && composition != VehicleComposition.Rigid && cnh != CnhCategory.E)
        {
            issues.Add(new("CNH_CATEGORY", ComplianceSeverity.Blocking,
                $"{composition.ToLabel()} exige CNH categoria E; o motorista tem categoria {cnh}."));
        }

        if (vehicle.DriverHasEar == false)
        {
            issues.Add(new("EAR_MISSING", ComplianceSeverity.Blocking,
                "A CNH do motorista não tem a observação EAR (exerce atividade remunerada)."));
        }
        else if (vehicle.DriverHasEar is null)
        {
            issues.Add(new("EAR_MISSING", ComplianceSeverity.Warning, "Não informado se a CNH do motorista tem EAR."));
        }

        AddExpiry(issues, "TOXICOLOGICAL", vehicle.DriverToxicologicalExamValidUntil, today,
            "Validade do exame toxicológico do motorista não informada.",
            date => $"Exame toxicológico do motorista vencido em {date}.",
            date => $"Exame toxicológico do motorista vence em {date}.");
    }

    private static void AddExpiry(
        List<ComplianceIssue> issues, string code, DateOnly? validUntil, DateOnly today,
        string missing, Func<string, string> expired, Func<string, string> expiring)
    {
        if (validUntil is not DateOnly date)
        {
            issues.Add(new($"{code}_MISSING", ComplianceSeverity.Warning, missing));
        }
        else if (date < today)
        {
            issues.Add(new($"{code}_EXPIRED", ComplianceSeverity.Blocking, expired(Format(date))));
        }
        else if (date <= today.AddDays(ExpiryWarningDays))
        {
            issues.Add(new($"{code}_EXPIRING", ComplianceSeverity.Warning, expiring(Format(date))));
        }
    }

    private static string Format(DateOnly date) => date.ToString("dd/MM/yyyy", PtBr);
}
