using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

/// <summary>
/// Implements POST /api/leiloes: atomically consolidates a set of Available route
/// segments into a new route and opens an auction for it.
///
/// Atomicity note: <see cref="IConsolidatedRouteRepository.AddAsync"/> and
/// <see cref="IAuctionRepository.AddAsync"/> only stage their entities on the shared,
/// per-request <c>DbContext</c> (they do not call SaveChanges). The route segments
/// loaded below are tracked entities from the same context, so mutating their
/// Status/RouteId is also just staged. Every staged change — the new route, the
/// updated segments, the new auction — is committed together by the single
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call at the end, which EF Core wraps in
/// one database transaction. If anything fails before that call, nothing is written.
/// </summary>
public class AuctionService : IAuctionService
{
    private readonly RouteSegmentAggregator _aggregator;
    private readonly IConsolidatedRouteRepository _routeRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuctionService(
        RouteSegmentAggregator aggregator,
        IConsolidatedRouteRepository routeRepository,
        IAuctionRepository auctionRepository,
        IUnitOfWork unitOfWork)
    {
        _aggregator = aggregator;
        _routeRepository = routeRepository;
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateAuctionResponseDto> CreateAsync(Guid contractorId, CreateAuctionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var segmentIds = RoutePreviewService.ValidateSegmentIds(dto.TrechoIds);
        var validated = Validate(dto);

        var (segmentsInOrder, aggregation) = await _aggregator.LoadAndAggregateAsync(
            contractorId, segmentIds, asTracking: true, cancellationToken);

        var unavailable = segmentsInOrder.FirstOrDefault(s => s.Status != SegmentStatus.Available);
        if (unavailable is not null)
        {
            throw new RouteSegmentUnavailableException(unavailable.Id);
        }

        var (pisoAnttEstimado, _) = RouteFinancials.EstimateAnttFloor(aggregation.TotalDistanceKm);
        var openedAt = DateTimeOffset.UtcNow;

        var route = new ConsolidatedRoute
        {
            ContractorId = contractorId,
            Status = ConsolidatedRouteStatus.InAuction,
            TotalDistanceKm = aggregation.TotalDistanceKm,
            EstimatedTimeHours = aggregation.TotalTimeHours,
            ConsolidatedBudgetCeiling = aggregation.ConsolidatedBudgetCeiling,
            EstimatedAnttFloor = pisoAnttEstimado,
            TotalWeightKg = aggregation.TotalWeightKg,
            TotalVolumeM3 = aggregation.TotalVolumeM3
        };
        await _routeRepository.AddAsync(route, cancellationToken);

        foreach (var segment in segmentsInOrder)
        {
            segment.Status = SegmentStatus.Routed;
            segment.RouteId = route.Id;
        }

        var auction = new Auction
        {
            RouteId = route.Id,
            OpenedAt = openedAt,
            ExpiresAt = validated.ExpiresAt,
            AutomaticAward = validated.AutomaticAward,
            Status = AuctionStatus.Open
        };
        await _auctionRepository.AddAsync(auction, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(auction, route, segmentsInOrder.Count);
    }

    private static ValidatedRequest Validate(CreateAuctionRequestDto dto)
    {
        var errors = new List<ValidationError>();

        if (dto.ExpiraEm is null)
        {
            errors.Add(new ValidationError("expiraEm", "Obrigatório."));
        }
        else if (dto.ExpiraEm <= DateTimeOffset.UtcNow)
        {
            errors.Add(new ValidationError("expiraEm", "Deve ser uma data futura."));
        }

        if (dto.AdjudicacaoAutomatica is null)
        {
            errors.Add(new ValidationError("adjudicacaoAutomatica", "Obrigatório."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors, "Não foi possível criar o leilão.");
        }

        return new ValidatedRequest(dto.ExpiraEm!.Value, dto.AdjudicacaoAutomatica!.Value);
    }

    private static CreateAuctionResponseDto MapToResponse(Auction auction, ConsolidatedRoute route, int segmentsUpdated)
    {
        var auctionDto = new AuctionDto(
            auction.Id,
            auction.RouteId,
            auction.OpenedAt,
            auction.ExpiresAt,
            auction.AutomaticAward,
            auction.Status.ToWire());

        var routeDto = new ConsolidatedRouteSummaryDto(
            route.Id,
            route.Status.ToWire(),
            route.TotalDistanceKm,
            route.EstimatedTimeHours,
            route.TotalWeightKg,
            route.TotalVolumeM3,
            route.ConsolidatedBudgetCeiling,
            route.EstimatedAnttFloor);

        return new CreateAuctionResponseDto(auctionDto, routeDto, segmentsUpdated);
    }

    private sealed record ValidatedRequest(DateTimeOffset ExpiresAt, bool AutomaticAward);
}
