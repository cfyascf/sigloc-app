using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class AuctionService : IAuctionService
{
    private readonly IRouteSegmentRepository _segmentRepository;
    private readonly IConsolidatedRouteRepository _routeRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IRouteGeocodingService _geocodingService;
    private readonly IAuctionNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;

    public AuctionService(
        IRouteSegmentRepository segmentRepository,
        IConsolidatedRouteRepository routeRepository,
        IAuctionRepository auctionRepository,
        IRouteGeocodingService geocodingService,
        IAuctionNotifier notifier,
        IUnitOfWork unitOfWork)
    {
        _segmentRepository = segmentRepository;
        _routeRepository = routeRepository;
        _auctionRepository = auctionRepository;
        _geocodingService = geocodingService;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateAuctionResponseDto> CreateAsync(Guid contractorId, CreateAuctionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var segmentIds = RoutePreviewService.ValidateSegmentIds(dto.SegmentIds);
        var expiresAt = ValidateExpiry(dto.ExpiresAt);

        // Load tracked segments so they can be updated inside the transaction.
        var segments = await _segmentRepository.GetByIdsAsync(contractorId, segmentIds, tracked: true, cancellationToken);

        var byId = segments.ToDictionary(s => s.Id);
        var missing = segmentIds.Where(id => !byId.ContainsKey(id)).Distinct().ToList();
        if (missing.Count > 0)
        {
            throw new RouteSegmentsNotFoundException(missing);
        }

        // Every segment must still be available (not already linked to another route).
        foreach (var id in segmentIds)
        {
            var segment = byId[id];
            if (segment.Status != SegmentStatus.Available || segment.RouteId is not null)
            {
                throw new SegmentUnavailableException(id);
            }
        }

        var aggregate = await RouteAggregator.AggregateAsync(segmentIds, segments, _geocodingService, cancellationToken);

        var openedAt = DateTimeOffset.UtcNow;

        var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var route = new ConsolidatedRoute
            {
                Id = Guid.NewGuid(),
                ContractorId = contractorId,
                Status = RouteStatus.InAuction,
                TotalDistanceKm = aggregate.TotalDistanceKm,
                EstimatedTimeHours = aggregate.EstimatedTimeHours,
                ConsolidatedCeiling = aggregate.ConsolidatedCeiling,
                EstimatedAnttFloor = aggregate.EstimatedAnttFloor,
                TotalWeightKg = aggregate.TotalWeightKg,
                TotalVolumeM3 = aggregate.TotalVolumeM3
            };

            await _routeRepository.AddAsync(route, ct);

            foreach (var id in segmentIds)
            {
                var segment = byId[id];
                segment.Status = SegmentStatus.Routed;
                segment.RouteId = route.Id;
            }

            var auction = new Auction
            {
                Id = Guid.NewGuid(),
                RouteId = route.Id,
                OpenedAt = openedAt,
                ExpiresAt = expiresAt,
                AutomaticAward = dto.AutomaticAward,
                Status = AuctionStatus.Open
            };

            await _auctionRepository.AddAsync(auction, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return (route, auction);
        }, cancellationToken);

        await _notifier.AuctionOpenedAsync(result.auction.Id, result.route.Id, contractorId, cancellationToken);

        return MapToResponse(result.auction, result.route, segmentIds.Count);
    }

    private static DateTimeOffset ValidateExpiry(DateTimeOffset? expiresAt)
    {
        if (expiresAt is null)
        {
            throw new ValidationException(
                new[] { new ValidationError("expiresAt", "Required.") },
                "Could not start the auction.");
        }

        if (expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new ValidationException(
                new[] { new ValidationError("expiresAt", "Must be in the future.") },
                "Could not start the auction.");
        }

        return expiresAt.Value;
    }

    private static CreateAuctionResponseDto MapToResponse(Auction auction, ConsolidatedRoute route, int updatedSegments)
    {
        return new CreateAuctionResponseDto(
            new AuctionDto(
                auction.Id,
                auction.RouteId,
                auction.OpenedAt,
                auction.ExpiresAt,
                auction.AutomaticAward,
                auction.Status.ToWire()),
            new ConsolidatedRouteDto(
                route.Id,
                route.Status.ToWire(),
                route.TotalDistanceKm,
                route.EstimatedTimeHours,
                route.TotalWeightKg,
                route.TotalVolumeM3,
                route.ConsolidatedCeiling,
                route.EstimatedAnttFloor),
            updatedSegments);
    }
}
