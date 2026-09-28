using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class AuctionService : IAuctionService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IRouteSegmentRepository _segmentRepository;
    private readonly IConsolidatedRouteRepository _routeRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IBidRepository _bidRepository;
    private readonly IRouteGeocodingService _geocodingService;
    private readonly IAuctionNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;

    public AuctionService(
        IRouteSegmentRepository segmentRepository,
        IConsolidatedRouteRepository routeRepository,
        IAuctionRepository auctionRepository,
        IBidRepository bidRepository,
        IRouteGeocodingService geocodingService,
        IAuctionNotifier notifier,
        IUnitOfWork unitOfWork)
    {
        _segmentRepository = segmentRepository;
        _routeRepository = routeRepository;
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
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

    public async Task<PagedAuctionsDto> SearchAsync(Guid contractorId, AuctionQueryDto query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize
        };

        var status = ParseStatus(query.Status);
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var (items, totalItems) = await _auctionRepository.SearchAsync(
            contractorId, search, status, page, pageSize, cancellationToken);

        var auctionIds = items.Select(i => i.Auction.Id).ToList();
        var metricsById = (await _bidRepository.GetMetricsForAuctionsAsync(auctionIds, cancellationToken))
            .ToDictionary(m => m.AuctionId);

        var now = DateTimeOffset.UtcNow;
        var listItems = items.Select(item =>
        {
            var stops = TravelPlanBuilder.Build(item.Segments);
            var cities = TravelPlanBuilder.OrderedCities(stops);

            metricsById.TryGetValue(item.Auction.Id, out var metrics);
            var totalBids = metrics?.TotalBids ?? 0;
            var bestBid = metrics?.BestBid;

            var earliestPickup = item.Segments.Count > 0
                ? item.Segments.Min(s => s.PickupDeadline)
                : (DateTimeOffset?)null;

            var risk = AuctionRiskCalculator.Evaluate(
                earliestPickup, item.Auction.ExpiresAt, item.Auction.Status, totalBids, now);

            var itinerarySummary = ItineraryFormatter.Summary(cities);
            var displayName = string.IsNullOrWhiteSpace(item.Auction.Name)
                ? itinerarySummary
                : item.Auction.Name!;

            return new AuctionListItemDto(
                Id: item.Auction.Id,
                RouteId: item.Route.Id,
                Status: item.Auction.Status.ToWire(),
                ItinerarySummary: displayName,
                ItineraryWithStates: ItineraryFormatter.WithStates(cities),
                LinkedSegments: item.Segments.Select(s => s.Id).ToList(),
                RiskIndicator: risk,
                ExpiresAt: item.Auction.ExpiresAt,
                BidMetrics: new BidMetricsDto(bestBid, totalBids));
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedAuctionsDto(listItems, page, pageSize, totalItems, totalPages);
    }

    public async Task<AuctionDetailDto> GetDetailAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default)
    {
        var detail = await _auctionRepository.GetDetailAsync(id, contractorId, cancellationToken)
            ?? throw new AuctionNotFoundException(id);

        var stops = TravelPlanBuilder.Build(detail.Segments);

        var totalBids = (await _bidRepository.GetMetricsForAuctionsAsync(new[] { id }, cancellationToken))
            .FirstOrDefault()?.TotalBids ?? 0;

        var bestBid = await _bidRepository.GetBestBidWithCarrierAsync(id, cancellationToken);
        BestBidDto? bestBidDto = bestBid is null
            ? null
            : new BestBidDto(bestBid.TotalValue, bestBid.CarrierName);

        var products = detail.Segments
            .SelectMany(s => s.Items)
            .Where(i => i.Product is not null)
            .Select(i => i.Product!);
        var vehicleRequirement = ConsolidatedVehicleRequirement.From(products);

        var segments = detail.Segments.Select(s => new AuctionSegmentDto(
            Id: s.Id,
            MainProduct: ResolveMainProduct(s),
            Origin: CityName(s.OriginAddress),
            Destination: CityName(s.DestinationAddress),
            FinancialCeiling: s.BudgetCeiling)).ToList();

        var route = detail.Route;
        var displayName = string.IsNullOrWhiteSpace(detail.Auction.Name)
            ? FormatRouteName(stops)
            : detail.Auction.Name!;
        var routeDto = new AuctionDetailRouteDto(
            Id: route.Id,
            Status: route.Status.ToWire(),
            FormattedName: displayName,
            TotalDistanceKm: route.TotalDistanceKm,
            TotalWeightKg: route.TotalWeightKg,
            TotalVolumeM3: route.TotalVolumeM3,
            ConsolidatedVehicleRequirement: FormatVehicleRequirement(vehicleRequirement),
            FinancialScenario: new FinancialScenarioDto(route.ConsolidatedCeiling, route.EstimatedAnttFloor));

        return new AuctionDetailDto(
            Id: detail.Auction.Id,
            Status: detail.Auction.Status.ToWire(),
            ExpiresAt: detail.Auction.ExpiresAt,
            Route: routeDto,
            BidMetrics: new DetailBidMetricsDto(totalBids, bestBidDto),
            TravelPlan: stops,
            Segments: segments);
    }

    public async Task<UpdateAuctionResponseDto> UpdateAsync(Guid contractorId, Guid id, UpdateAuctionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var auction = await _auctionRepository.GetTrackedByIdAsync(id, contractorId, cancellationToken)
            ?? throw new AuctionNotFoundException(id);

        if (dto.ExpiresAt is not null)
        {
            auction.ExpiresAt = ValidateExpiry(dto.ExpiresAt);
        }

        await _auctionRepository.UpdateAsync(auction, cancellationToken);

        return new UpdateAuctionResponseDto(auction.Id, auction.ExpiresAt);
    }

    public async Task DeleteAsync(Guid contractorId, Guid id, CancellationToken cancellationToken = default)
    {
        var auction = await _auctionRepository.GetTrackedByIdAsync(id, contractorId, cancellationToken)
            ?? throw new AuctionNotFoundException(id);

        await _auctionRepository.DeleteAsync(auction, cancellationToken);
    }

    private static AuctionStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return AuctionStatus.Open;
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "OPEN" => AuctionStatus.Open,
            "CLOSED" => AuctionStatus.Closed,
            "CANCELLED" => AuctionStatus.Cancelled,
            _ => AuctionStatus.Open
        };
    }

    /// <summary>Main product of a segment: the one carried in the largest quantity.</summary>
    private static string ResolveMainProduct(RouteSegment segment)
    {
        var main = segment.Items
            .Where(i => i.Product is not null)
            .OrderByDescending(i => i.Quantity)
            .Select(i => i.Product!.Name)
            .FirstOrDefault();

        return main ?? "—";
    }

    /// <summary>Human-friendly route name derived from the first and last travel-plan cities.</summary>
    private static string FormatRouteName(IReadOnlyList<TravelPlanStopDto> stops)
    {
        if (stops.Count == 0)
        {
            return "Rota Consolidada";
        }

        var origin = CityName(stops[0].CityState);
        var destination = CityName(stops[^1].CityState);
        return origin == destination
            ? $"Rota {origin}"
            : $"Rota {origin} → {destination}";
    }

    private static string CityName(string cityState)
    {
        var comma = cityState.IndexOf(',');
        return (comma >= 0 ? cityState[..comma] : cityState).Trim();
    }

    /// <summary>Condenses the consolidated vehicle requirement into a single display string.</summary>
    private static string FormatVehicleRequirement(ConsolidatedVehicleRequirement requirement)
    {
        var parts = new List<string> { requirement.BaseBodyworkType };

        if (!string.Equals(requirement.MinRefrigerationLevel, "Nenhuma", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(requirement.MinRefrigerationLevel);
        }

        if (requirement.RequiresMopp)
        {
            parts.Add("MOPP");
        }

        if (requirement.RequiresCargoFixing)
        {
            parts.Add("Fixação de Carga");
        }

        return string.Join(" · ", parts);
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
