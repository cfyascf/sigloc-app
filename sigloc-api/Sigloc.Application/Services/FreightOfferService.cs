using System.Globalization;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class FreightOfferService : IFreightOfferService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IAuctionRepository _auctionRepository;
    private readonly IBidRepository _bidRepository;

    public FreightOfferService(IAuctionRepository auctionRepository, IBidRepository bidRepository)
    {
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
    }

    public async Task<PagedFreightOffersDto> ListAsync(
        Guid carrierId,
        FreightOfferQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize
        };

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var now = DateTimeOffset.UtcNow;

        var (items, totalItems) = await _auctionRepository.SearchAvailableForCarrierAsync(
            carrierId, search, now, page, pageSize, cancellationToken);

        var auctionIds = items.Select(i => i.Auction.Id).ToList();

        var metricsById = (await _bidRepository.GetMetricsForAuctionsAsync(auctionIds, cancellationToken))
            .ToDictionary(m => m.AuctionId);
        var bidStatuses = await _bidRepository.GetCarrierBidStatusesAsync(carrierId, auctionIds, cancellationToken);

        var riskFilter = NormalizeFilterValue(query.Risk);

        var listItems = items
            .Select(item => MapListItem(item, metricsById, bidStatuses, now))
            .Where(dto => riskFilter is null || string.Equals(dto.Risk, riskFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new PagedFreightOffersDto(listItems, page, pageSize, totalItems);
    }

    public async Task<FreightOfferDetailDto> GetByIdAsync(
        Guid carrierId,
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var detail = await _auctionRepository.GetAvailableForCarrierAsync(offerId, carrierId, now, cancellationToken)
            ?? throw new OfferNotFoundException(offerId);

        var stops = TravelPlanBuilder.Build(detail.Segments);
        var cities = TravelPlanBuilder.OrderedCities(stops);

        var totalBids = (await _bidRepository.GetMetricsForAuctionsAsync(new[] { offerId }, cancellationToken))
            .FirstOrDefault()?.TotalBids ?? 0;

        var requirements = BuildRequirementTags(detail.Segments);
        var firstSegmentId = FirstSegmentId(detail.Segments);

        return new FreightOfferDetailDto(
            Id: detail.Auction.Id.ToString(),
            SegmentId: firstSegmentId,
            Contractor: detail.ContractorName,
            RouteLabel: ItineraryFormatter.WithStates(cities),
            Stops: stops.Select(s => s.CityState).ToList(),
            TargetValue: detail.Route.ConsolidatedCeiling,
            TotalBids: totalBids,
            Requirements: requirements,
            Details: BuildDetails(detail.Route, requirements));
    }

    private static FreightOfferListItemDto MapListItem(
        CarrierAuctionWithRoute item,
        IReadOnlyDictionary<Guid, AuctionBidMetrics> metricsById,
        IReadOnlyDictionary<Guid, BidStatus> bidStatuses,
        DateTimeOffset now)
    {
        var stops = TravelPlanBuilder.Build(item.Segments);
        var cities = TravelPlanBuilder.OrderedCities(stops);

        metricsById.TryGetValue(item.Auction.Id, out var metrics);
        var totalBids = metrics?.TotalBids ?? 0;

        var earliestPickup = item.Segments.Count > 0
            ? item.Segments.Min(s => s.PickupDeadline)
            : (DateTimeOffset?)null;
        var lastDelivery = item.Segments.Count > 0
            ? item.Segments.Max(s => s.DeliveryDeadline)
            : (DateTimeOffset?)null;

        var risk = ToWireRisk(AuctionRiskCalculator.Evaluate(
            earliestPickup, item.Auction.ExpiresAt, item.Auction.Status, totalBids, now));

        var hoursLeft = Math.Max(0d, Math.Round((item.Auction.ExpiresAt - now).TotalHours, 1));
        var isExpiringSoon = hoursLeft <= 4d;

        var bidStatus = bidStatuses.TryGetValue(item.Auction.Id, out var status)
            ? status.ToWire()
            : null;

        return new FreightOfferListItemDto(
            Id: item.Auction.Id.ToString(),
            Contractor: item.ContractorName,
            RouteLabel: ItineraryFormatter.WithStates(cities),
            TargetValue: item.Route.ConsolidatedCeiling,
            TotalBids: totalBids,
            PickupLabel: FormatDeadline(earliestPickup),
            EtaLabel: FormatDeadline(lastDelivery),
            TotalWeight: FormatWeight(item.Route.TotalWeightKg),
            TotalVolume: FormatVolume(item.Route.TotalVolumeM3),
            Requirements: BuildRequirementTags(item.Segments),
            IsExpiringSoon: isExpiringSoon,
            HoursLeft: hoursLeft,
            SegmentId: FirstSegmentId(item.Segments),
            Risk: risk,
            BidStatus: bidStatus);
    }

    /// <summary>
    /// Consolidates the "strongest restriction wins" tags for the whole route. Applied on
    /// every product carried on every linked segment. Falls back to "Carga Seca" when no
    /// stronger restriction applies.
    /// </summary>
    private static IReadOnlyList<string> BuildRequirementTags(IReadOnlyList<RouteSegment> segments)
    {
        var products = segments
            .SelectMany(s => s.Items)
            .Where(i => i.Product is not null)
            .Select(i => i.Product!)
            .ToList();

        if (products.Count == 0)
        {
            return new[] { "Carga Seca" };
        }

        var environment = TransportEnvironment.Dry;
        var dangerous = false;
        var fragile = false;

        foreach (var product in products)
        {
            if (product.TransportEnvironment > environment)
            {
                environment = product.TransportEnvironment;
            }

            dangerous |= product.Dangerous;
            fragile |= product.Fragile;
        }

        var tags = new List<string>();

        switch (environment)
        {
            case TransportEnvironment.Frozen:
                tags.Add("Congelado");
                break;
            case TransportEnvironment.Chilled:
                tags.Add("Refrigerado");
                break;
            default:
                tags.Add("Carga Seca");
                break;
        }

        if (dangerous)
        {
            tags.Add("Perigoso");
        }

        if (fragile)
        {
            tags.Add("Frágil");
        }

        return tags;
    }

    private static string BuildDetails(ConsolidatedRoute route, IReadOnlyList<string> requirements)
    {
        var distance = route.TotalDistanceKm.ToString("0", PtBr);
        var weight = FormatWeight(route.TotalWeightKg);
        var volume = FormatVolume(route.TotalVolumeM3);
        return $"Distância total {distance} km · {weight} · {volume} · Exigências: {string.Join(", ", requirements)}.";
    }

    private static string? FirstSegmentId(IReadOnlyList<RouteSegment> segments)
        => segments.Count == 0
            ? null
            : segments.OrderBy(s => s.PickupDeadline).First().Id.ToString();

    private static string FormatDeadline(DateTimeOffset? deadline)
        => deadline?.ToString("dd/MM/yyyy HH:mm", PtBr) ?? "—";

    private static string FormatWeight(double weightKg)
        => weightKg >= 1000
            ? $"{(weightKg / 1000).ToString("0.##", PtBr)} ton"
            : $"{weightKg.ToString("0.##", PtBr)} kg";

    private static string FormatVolume(double volumeM3)
        => $"{volumeM3.ToString("0.##", PtBr)} m³";

    /// <summary>Normalizes the internal CRITICAL indicator to the CRITIC value the UI uses.</summary>
    private static string ToWireRisk(string risk)
        => string.Equals(risk, AuctionRiskCalculator.Critical, StringComparison.OrdinalIgnoreCase)
            ? "CRITIC"
            : risk;

    private static string? NormalizeFilterValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
