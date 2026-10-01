using Sigloc.Domain.Entities;

namespace Sigloc.Application.Contracts;

public record AnttFreightFloorResult(decimal MinimumFreight, decimal DisplacementCostPerKm, decimal LoadingUnloadingCost, DateTimeOffset CalculatedAt);

public interface IAnttFreightFloorService
{
    Task<AnttFreightFloorResult?> CalculateAsync(
        double distanceKm,
        IReadOnlyCollection<Product> products,
        int axleCount,
        CancellationToken cancellationToken = default);
}
