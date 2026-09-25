using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class RoutePreviewService : IRoutePreviewService
{
    private readonly IRouteSegmentRepository _segmentRepository;
    private readonly IRouteGeocodingService _geocodingService;

    public RoutePreviewService(
        IRouteSegmentRepository segmentRepository,
        IRouteGeocodingService geocodingService)
    {
        _segmentRepository = segmentRepository;
        _geocodingService = geocodingService;
    }

    public async Task<RoutePreviewResponseDto> PreviewAsync(Guid contractorId, RoutePreviewRequestDto dto, CancellationToken cancellationToken = default)
    {
        var segmentIds = ValidateSegmentIds(dto.SegmentIds);

        var segments = await _segmentRepository.GetByIdsAsync(contractorId, segmentIds, tracked: false, cancellationToken);

        var aggregate = await RouteAggregator.AggregateAsync(segmentIds, segments, _geocodingService, cancellationToken);

        return new RoutePreviewResponseDto(
            aggregate.TotalDistanceKm,
            aggregate.EstimatedTimeHours,
            aggregate.ConsolidatedCeiling,
            aggregate.EstimatedAnttFloor,
            aggregate.EstimatedToll,
            aggregate.CostPerKm,
            new RouteAggregatedTotalsDto(aggregate.TotalWeightKg, aggregate.TotalVolumeM3),
            new ConsolidatedVehicleRequirementDto(
                aggregate.VehicleRequirement.BaseBodyworkType,
                aggregate.VehicleRequirement.MinRefrigerationLevel,
                aggregate.VehicleRequirement.RequiresMopp,
                aggregate.VehicleRequirement.RequiresCargoFixing));
    }

    internal static IReadOnlyList<Guid> ValidateSegmentIds(IReadOnlyList<Guid>? segmentIds)
    {
        var ids = segmentIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0)
        {
            throw new ValidationException(
                new[] { new ValidationError("segmentIds", "At least one route segment is required.") },
                "Could not process the route.");
        }

        return ids;
    }
}
