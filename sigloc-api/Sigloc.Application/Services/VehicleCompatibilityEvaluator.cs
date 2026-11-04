using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Application.Services;

/// <summary>
/// Audits a vehicle against the consolidated cargo requirement of a route (the Constraint
/// Engine's equipment/compliance trava). Evaluates bodywork type, refrigeration level,
/// MOPP and cargo fixing, returning the first blocking reason found (or null when the
/// vehicle is fully compatible). The rules themselves live in <see cref="CargoCompatibility"/>.
/// </summary>
internal static class VehicleCompatibilityEvaluator
{
    /// <summary>
    /// Returns a <see cref="BidRejectedException"/> describing the equipment incompatibility,
    /// or null when the vehicle satisfies every consolidated requirement.
    /// </summary>
    public static BidRejectedException? Evaluate(Vehicle vehicle, ConsolidatedVehicleRequirement requirement)
    {
        if (!requirement.AcceptedBodyTypes.Contains(vehicle.BodyType))
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                $"Incompatibilidade de Equipamento: a carroceria {vehicle.BodyType.ToLabel()} não atende esta carga "
                + $"(aceitas: {requirement.BaseBodyworkType}).");
        }

        var requiredRefrigeration = ParseRefrigeration(requirement.MinRefrigerationLevel);
        if (CargoCompatibility.EffectiveRefrigeration(vehicle) < requiredRefrigeration)
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                $"Incompatibilidade de Equipamento: exigido {requirement.MinRefrigerationLevel} (refrigeração).");
        }

        if (requirement.RequiresMopp && !vehicle.HasMopp)
        {
            return new BidRejectedException(
                BidRejectionCode.EquipmentMismatch,
                "Incompatibilidade de Equipamento: exigido motorista com MOPP (carga perigosa).");
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
    private static RefrigerationLevel ParseRefrigeration(string minRefrigerationLevel) =>
        Enum.TryParse<RefrigerationLevel>(minRefrigerationLevel, out var level) ? level : RefrigerationLevel.Nenhuma;
}
