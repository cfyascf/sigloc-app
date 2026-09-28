using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>
/// Audits a vehicle against the consolidated cargo requirement of a route (the Constraint
/// Engine's equipment/compliance trava). Evaluates refrigeration level, bodywork type,
/// MOPP and cargo fixing, returning the first blocking reason found (or null when the
/// vehicle is fully compatible).
/// </summary>
internal static class VehicleCompatibilityEvaluator
{
    /// <summary>
    /// Returns a <see cref="BidRejectedException"/> describing the equipment incompatibility,
    /// or null when the vehicle satisfies every consolidated requirement.
    /// </summary>
    public static BidRejectedException? Evaluate(Vehicle vehicle, ConsolidatedVehicleRequirement requirement)
    {
        var requiredRefrigeration = ParseRefrigeration(requirement.MinRefrigerationLevel);
        if (vehicle.RefrigerationLevel < requiredRefrigeration)
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                $"Incompatibilidade de Equipamento: exigido {requirement.MinRefrigerationLevel} (refrigeração).");
        }

        if (!IsBodyworkCompatible(vehicle.BodyType, requirement.BaseBodyworkType))
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                $"Incompatibilidade de Equipamento: exigido {requirement.BaseBodyworkType}.");
        }

        if (requirement.RequiresMopp && !vehicle.HasMopp)
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                "Incompatibilidade de Equipamento: exigido certificado MOPP (carga perigosa).");
        }

        if (requirement.RequiresCargoFixing && !vehicle.HasCargoSecuring)
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                "Incompatibilidade de Equipamento: exigida fixação de carga (carga frágil).");
        }

        return null;
    }

    /// <summary>Maps the consolidated refrigeration label back to the vehicle enum scale.</summary>
    private static RefrigerationLevel ParseRefrigeration(string minRefrigerationLevel) => minRefrigerationLevel switch
    {
        "Congelado" => RefrigerationLevel.Congelado,
        "Resfriado" => RefrigerationLevel.Resfriado,
        _ => RefrigerationLevel.Nenhuma
    };

    /// <summary>
    /// Checks whether the vehicle bodywork can carry the consolidated bodywork requirement.
    /// The consolidated requirement is one of "Baú / Sider", "Graneleiro / Basculante" or
    /// "Tanque"; refrigerated bodies also satisfy the dry "Baú / Sider" family.
    /// </summary>
    private static bool IsBodyworkCompatible(VehicleBodyType bodyType, string requiredBodywork) => requiredBodywork switch
    {
        "Baú / Sider" => bodyType is VehicleBodyType.Bau
            or VehicleBodyType.Sider
            or VehicleBodyType.GradeBaixa
            or VehicleBodyType.Frigorifico,
        "Graneleiro / Basculante" => bodyType is VehicleBodyType.Cacamba,
        "Tanque" => false,
        _ => true
    };
}
