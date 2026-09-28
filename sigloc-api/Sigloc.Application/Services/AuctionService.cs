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
    private readonly ITripRepository _tripRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IBlockedBidAttemptRepository _blockedBidAttemptRepository;
    private readonly IRouteGeocodingService _geocodingService;
    private readonly IAuctionNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;

    public AuctionService(
        IRouteSegmentRepository segmentRepository,
        IConsolidatedRouteRepository routeRepository,
        IAuctionRepository auctionRepository,
        IBidRepository bidRepository,
        ITripRepository tripRepository,
        IVehicleRepository vehicleRepository,
        IBlockedBidAttemptRepository blockedBidAttemptRepository,
        IRouteGeocodingService geocodingService,
        IAuctionNotifier notifier,
        IUnitOfWork unitOfWork)
    {
        _segmentRepository = segmentRepository;
        _routeRepository = routeRepository;
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
        _tripRepository = tripRepository;
        _vehicleRepository = vehicleRepository;
        _blockedBidAttemptRepository = blockedBidAttemptRepository;
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

    public async Task<BidRankingDto> GetBidRankingAsync(Guid contractorId, Guid auctionId, CancellationToken cancellationToken = default)
    {
        // Loading the detail also enforces the contractor scope (404 otherwise).
        var detail = await _auctionRepository.GetDetailAsync(auctionId, contractorId, cancellationToken)
            ?? throw new AuctionNotFoundException(auctionId);

        var stops = TravelPlanBuilder.Build(detail.Segments);
        var cities = TravelPlanBuilder.OrderedCities(stops);

        var firstPickup = detail.Segments.Count > 0
            ? detail.Segments.Min(s => s.PickupDeadline)
            : (DateTimeOffset?)null;
        var lastDelivery = detail.Segments.Count > 0
            ? detail.Segments.Max(s => s.DeliveryDeadline)
            : (DateTimeOffset?)null;

        var ceiling = detail.Route.ConsolidatedCeiling;

        var orderedSegments = detail.Segments
            .OrderBy(s => s.PickupDeadline)
            .ToList();
        var linkedSegmentIds = orderedSegments.Select(s => s.Id).ToList();

        var routeSummary = new BidRankingRouteSummaryDto(
            RouteId: detail.Route.Id,
            ItinerarySummary: ItineraryFormatter.Summary(cities),
            ItineraryWithStates: ItineraryFormatter.WithStates(cities),
            SegmentCount: detail.Segments.Count,
            FirstSegmentId: linkedSegmentIds.Count > 0 ? linkedSegmentIds[0] : null,
            LinkedSegmentIds: linkedSegmentIds,
            FirstPickupDeadline: firstPickup,
            LastDeliveryDeadline: lastDelivery,
            FinancialScenario: new FinancialScenarioDto(ceiling, detail.Route.EstimatedAnttFloor));

        var ranked = await _bidRepository.GetRankedBidsAsync(auctionId, cancellationToken);

        var bids = ranked.Select((r, index) =>
        {
            var savingsValue = ceiling - r.Bid.TotalValue;
            var savingsPercentage = ceiling > 0
                ? Math.Round((double)(savingsValue / ceiling) * 100, 1)
                : 0d;

            return new BidRankingItemDto(
                Rank: index + 1,
                BidId: r.Bid.Id,
                Status: r.Bid.Status.ToWire(),
                SubmittedAt: r.Bid.SubmittedAt,
                Carrier: new BidRankingCarrierDto(
                    Id: r.Carrier.Id,
                    TradeName: string.IsNullOrWhiteSpace(r.Carrier.TradeName) ? r.Carrier.CompanyName : r.Carrier.TradeName!,
                    AverageRating: r.Carrier.AverageRating,
                    OnTimeDeliveryRate: r.Carrier.OnTimeDeliveryRate,
                    HasActiveInsurancePolicy: r.Carrier.HasActiveInsurancePolicy),
                Vehicle: new BidRankingVehicleDto(
                    Plate: r.Vehicle.Plate,
                    BodyType: BodyTypeLabel(r.Vehicle.BodyType)),
                Financials: new BidRankingFinancialsDto(
                    TotalValue: r.Bid.TotalValue,
                    NetFreightValue: r.Bid.NetFreightValue,
                    TollValue: r.Bid.TollValue,
                    SavingsValue: savingsValue,
                    SavingsPercentage: savingsPercentage));
        }).ToList();

        return new BidRankingDto(
            AuctionId: detail.Auction.Id,
            Status: detail.Auction.Status.ToWire(),
            Route: routeSummary,
            Bids: bids);
    }

    public async Task<AwardAuctionResponseDto> AwardAsync(Guid contractorId, Guid auctionId, AwardAuctionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var auction = await _auctionRepository.GetTrackedByIdAsync(auctionId, contractorId, cancellationToken)
            ?? throw new AuctionNotFoundException(auctionId);

        if (auction.Status != AuctionStatus.Open)
        {
            throw new AuctionNotOpenException(auctionId);
        }

        var bids = await _bidRepository.GetTrackedByAuctionAsync(auctionId, cancellationToken);
        var winner = bids.FirstOrDefault(b => b.Id == dto.WinningBidId)
            ?? throw new BidNotFoundException(dto.WinningBidId);

        var route = await _routeRepository.GetTrackedByIdAsync(auction.RouteId, cancellationToken)
            ?? throw new AuctionNotFoundException(auctionId);

        var trip = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1 & 2. Elect the winner and mark every other bid as a loser.
            foreach (var bid in bids)
            {
                bid.Status = bid.Id == winner.Id ? BidStatus.Winner : BidStatus.Losing;
            }

            // 3. Close the auction.
            auction.Status = AuctionStatus.Closed;

            // 4. Move the consolidated route to awaiting pickup.
            route.Status = RouteStatus.AwaitingPickup;

            // 5. Create the trip (Viagem) that starts the physical operation.
            var newTrip = new Trip
            {
                Id = Guid.NewGuid(),
                RouteId = route.Id,
                AuctionId = auction.Id,
                CarrierId = winner.CarrierId,
                VehicleId = winner.VehicleId,
                BidId = winner.Id,
                AgreedValue = winner.TotalValue,
                Status = TripStatus.AwaitingPickup
            };

            await _tripRepository.AddAsync(newTrip, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return newTrip;
        }, cancellationToken);

        return new AwardAuctionResponseDto(
            TripId: trip.Id,
            AuctionId: auction.Id,
            WinningBidId: winner.Id,
            AuctionStatus: auction.Status.ToWire(),
            RouteStatus: route.Status.ToWire());
    }

    public async Task<CarrierBidAnalysisDto> GetCarrierAnalysisAsync(
        Guid carrierId,
        Guid auctionId,
        CancellationToken cancellationToken = default)
    {
        // Scoped to the carrier's active partnerships (open, non-expired), mirroring the
        // opportunity board: a partner carrier can analyse the offer; others get a 404.
        var detail = await _auctionRepository.GetAvailableForCarrierAsync(auctionId, carrierId, DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new OfferNotFoundException(auctionId);

        var stops = TravelPlanBuilder.Build(detail.Segments);
        var cities = TravelPlanBuilder.OrderedCities(stops);

        var firstPickup = detail.Segments.Count > 0
            ? detail.Segments.Min(s => s.PickupDeadline)
            : (DateTimeOffset?)null;
        var lastDelivery = detail.Segments.Count > 0
            ? detail.Segments.Max(s => s.DeliveryDeadline)
            : (DateTimeOffset?)null;

        var totalBids = (await _bidRepository.GetMetricsForAuctionsAsync(new[] { auctionId }, cancellationToken))
            .FirstOrDefault()?.TotalBids ?? 0;
        var bestBid = await _bidRepository.GetBestBidWithCarrierAsync(auctionId, cancellationToken);

        var products = detail.Segments
            .SelectMany(s => s.Items)
            .Where(i => i.Product is not null)
            .Select(i => i.Product!)
            .ToList();
        var requirement = ConsolidatedVehicleRequirement.From(products);

        var displayName = string.IsNullOrWhiteSpace(detail.Auction.Name)
            ? FormatRouteName(stops)
            : detail.Auction.Name!;

        var fleet = await _vehicleRepository.GetAllAsync(carrierId, cancellationToken);

        var physical = new CarrierAnalysisPhysicalRequirementsDto(
            RecommendedFleet: FormatVehicleRequirement(requirement),
            ConsolidatedWeightKg: detail.Route.TotalWeightKg,
            VolumeM3: detail.Route.TotalVolumeM3,
            RequiredTemperature: ResolveRequiredTemperature(products, requirement),
            HandlingRestrictions: ResolveHandlingRestrictions(products));

        return new CarrierBidAnalysisDto(
            AuctionId: detail.Auction.Id,
            RouteSummary: new CarrierAnalysisRouteSummaryDto(
                ReferenceCode: FormatReferenceCode(detail.Route.Id),
                ShortItinerary: displayName,
                Sla: new CarrierAnalysisSlaDto(firstPickup, lastDelivery)),
            Competition: new CarrierAnalysisCompetitionDto(
                ActiveBids: totalBids,
                BestLeaderOffer: bestBid?.TotalValue,
                AuctionCeiling: detail.Route.ConsolidatedCeiling),
            PhysicalRequirements: physical,
            CarrierAvailableFleet: fleet
                .Select(v => new CarrierAnalysisVehicleDto(
                    VehicleId: v.Id,
                    Plate: v.Plate,
                    Model: v.Model,
                    CapacityWeightKg: v.CapacityWeight,
                    CapacityVolumeM3: v.CapacityVolume,
                    Specifications: BuildVehicleSpecifications(v)))
                .ToList(),
            TravelPlan: stops
                .Select(s => new CarrierAnalysisTravelStopDto(s.Order, s.CityState, s.ActionType))
                .ToList());
    }

    public async Task<PlaceBidResponseDto> PlaceBidAsync(
        Guid carrierId,
        Guid auctionId,
        PlaceBidRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // Scoped to the carrier's active partnerships (open, non-expired). A non-partner
        // carrier, a closed or an expired auction all resolve to a 404 offer-not-found.
        var detail = await _auctionRepository.GetAvailableForCarrierAsync(auctionId, carrierId, DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new OfferNotFoundException(auctionId);

        var route = detail.Route;

        // --- 1. Financial trava --------------------------------------------------
        if (dto.ValorOferecido <= 0)
        {
            throw new BidRejectedException(
                BidRejectionCode.InvalidValue,
                "O valor oferecido deve ser maior que zero.");
        }

        if (dto.ValorOferecido > route.ConsolidatedCeiling)
        {
            throw new BidRejectedException(
                BidRejectionCode.AboveCeiling,
                $"Valor acima do teto do leilão ({route.ConsolidatedCeiling:C}).");
        }

        // The vehicle must belong to the carrier placing the bid.
        var vehicle = await _vehicleRepository.GetByIdAsync(dto.VeiculoId, carrierId, cancellationToken)
            ?? throw new KeyNotFoundException($"Veículo {dto.VeiculoId} não encontrado para esta transportadora.");

        // --- 2. Physical trava: weight ------------------------------------------
        if ((decimal)route.TotalWeightKg > vehicle.CapacityWeight)
        {
            var excess = (decimal)route.TotalWeightKg - vehicle.CapacityWeight;
            await LogBlockedAttemptAsync(route.ContractorId, auctionId, carrierId, BlockedReason.Weight, cancellationToken);
            throw new BidRejectedException(
                BidRejectionCode.Overweight,
                $"Excesso de Peso: a carga excede a capacidade do veículo em {excess:N0} kg.");
        }

        // --- 3. Physical trava: volume ------------------------------------------
        if ((decimal)route.TotalVolumeM3 > vehicle.CapacityVolume)
        {
            var excess = (decimal)route.TotalVolumeM3 - vehicle.CapacityVolume;
            await LogBlockedAttemptAsync(route.ContractorId, auctionId, carrierId, BlockedReason.Volume, cancellationToken);
            throw new BidRejectedException(
                BidRejectionCode.Overvolume,
                $"Excesso de Volume: a carga excede a capacidade do veículo em {excess:N0} m³.");
        }

        // --- 4. Equipment / compliance trava ------------------------------------
        var products = detail.Segments
            .SelectMany(s => s.Items)
            .Where(i => i.Product is not null)
            .Select(i => i.Product!)
            .ToList();
        var requirement = ConsolidatedVehicleRequirement.From(products);

        var equipmentRejection = VehicleCompatibilityEvaluator.Evaluate(vehicle, requirement);
        if (equipmentRejection is not null)
        {
            await LogBlockedAttemptAsync(route.ContractorId, auctionId, carrierId, BlockedReason.Equipment, cancellationToken);
            throw equipmentRejection;
        }

        // --- 5. Temporal trava: anti-overbooking --------------------------------
        var newWindowStart = detail.Segments.Count > 0 ? detail.Segments.Min(s => s.PickupDeadline) : (DateTimeOffset?)null;
        var newWindowEnd = detail.Segments.Count > 0 ? detail.Segments.Max(s => s.DeliveryDeadline) : (DateTimeOffset?)null;

        if (newWindowStart is not null && newWindowEnd is not null)
        {
            var occupiedWindows = await _tripRepository.GetActiveWindowsForVehicleAsync(dto.VeiculoId, cancellationToken);
            var overlaps = occupiedWindows.Any(w => w.Start <= newWindowEnd.Value && newWindowStart.Value <= w.End);
            if (overlaps)
            {
                await LogBlockedAttemptAsync(route.ContractorId, auctionId, carrierId, BlockedReason.Sla, cancellationToken);
                throw new BidRejectedException(
                    BidRejectionCode.Overbooked,
                    "Overbooking: o veículo já possui uma viagem ativa que se sobrepõe à janela desta rota.");
            }
        }

        // --- All travas passed: persist the bid ---------------------------------
        var tollValue = detail.Segments.Sum(s => s.EstimatedTollCost);
        var totalValue = dto.ValorOferecido + tollValue;
        var submittedAt = DateTimeOffset.UtcNow;

        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            CarrierId = carrierId,
            VehicleId = dto.VeiculoId,
            NetFreightValue = dto.ValorOferecido,
            TollValue = tollValue,
            TotalValue = totalValue,
            SubmittedAt = submittedAt,
            Status = BidStatus.Pending
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _bidRepository.AddAsync(bid, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

        return new PlaceBidResponseDto(
            BidId: bid.Id,
            AuctionId: auctionId,
            NetFreightValue: bid.NetFreightValue,
            TollValue: bid.TollValue,
            TotalValue: bid.TotalValue,
            SubmittedAt: bid.SubmittedAt,
            Status: bid.Status.ToWire());
    }

    private Task LogBlockedAttemptAsync(
        Guid contractorId,
        Guid auctionId,
        Guid carrierId,
        BlockedReason reason,
        CancellationToken cancellationToken)
    {
        return _blockedBidAttemptRepository.AddAsync(new BlockedBidAttempt
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId,
            AuctionId = auctionId,
            CarrierId = carrierId,
            Reason = reason,
            AttemptedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    /// <summary>Human-friendly reference code derived from a route id (e.g. ROT-XXXXXXXX).</summary>
    private static string FormatReferenceCode(Guid routeId)
        => $"ROT-{routeId.ToString("N")[..8].ToUpperInvariant()}";

    /// <summary>Required temperature label built from the cargo environment and min/max temps.</summary>
    private static string ResolveRequiredTemperature(IReadOnlyList<Product> products, ConsolidatedVehicleRequirement requirement)
    {
        if (string.Equals(requirement.MinRefrigerationLevel, "Nenhuma", StringComparison.OrdinalIgnoreCase))
        {
            return "Ambiente (Sem refrigeração)";
        }

        var mins = products.Where(p => p.TempMin.HasValue).Select(p => p.TempMin!.Value).ToList();
        var maxes = products.Where(p => p.TempMax.HasValue).Select(p => p.TempMax!.Value).ToList();

        if (mins.Count > 0 || maxes.Count > 0)
        {
            var min = mins.Count > 0 ? mins.Min() : maxes.Min();
            var max = maxes.Count > 0 ? maxes.Max() : mins.Max();
            var range = Math.Abs(min - max) < 0.0001
                ? $"{min:0.#}°C"
                : $"{min:0.#}°C a {max:0.#}°C";
            return $"{range} ({requirement.MinRefrigerationLevel})";
        }

        return requirement.MinRefrigerationLevel;
    }

    /// <summary>Distinct handling restrictions inherited from the cargo (packaging/fragile/dangerous/notes).</summary>
    private static IReadOnlyList<string> ResolveHandlingRestrictions(IReadOnlyList<Product> products)
    {
        var restrictions = new List<string>();

        if (products.Any(p => p.PackagingType == PackagingType.Palletized))
        {
            restrictions.Add("Carga Paletizada");
        }

        if (products.Any(p => p.Fragile))
        {
            restrictions.Add("Não Empilhar");
        }

        if (products.Any(p => p.Dangerous))
        {
            restrictions.Add("Carga Perigosa (MOPP)");
        }

        foreach (var note in products
            .Select(p => p.HandlingRestriction)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!restrictions.Contains(note, StringComparer.OrdinalIgnoreCase))
            {
                restrictions.Add(note);
            }
        }

        return restrictions;
    }

    /// <summary>Specification tags of a vehicle shown in the fleet dropdown.</summary>
    private static IReadOnlyList<string> BuildVehicleSpecifications(Vehicle vehicle)
    {
        var specs = new List<string> { BodyTypeLabel(vehicle.BodyType) };

        if (vehicle.RefrigerationLevel != RefrigerationLevel.Nenhuma)
        {
            specs.Add(vehicle.RefrigerationLevel.ToString());
        }

        if (vehicle.HasMopp)
        {
            specs.Add("MOPP");
        }

        if (vehicle.HasCargoSecuring)
        {
            specs.Add("Fixação de Carga");
        }

        return specs;
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

    /// <summary>Human-friendly label of a vehicle body type, from its [Description] attribute.</summary>
    private static string BodyTypeLabel(VehicleBodyType bodyType)
    {
        var field = typeof(VehicleBodyType).GetField(bodyType.ToString());
        var description = field?
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .Cast<System.ComponentModel.DescriptionAttribute>()
            .FirstOrDefault();

        return description?.Description ?? bodyType.ToString();
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
