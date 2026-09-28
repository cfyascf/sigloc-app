namespace Sigloc.Application.DTOs;

// ---------------------------------------------------------------------------
// Opportunity board (Mural de Fretes) — carrier-facing freight offers
// GET /api/freight-offers and GET /api/freight-offers/{offerId}
// ---------------------------------------------------------------------------

/// <summary>Query parameters for the opportunity board listing.</summary>
public record FreightOfferQueryDto(
    string? Search,
    string? Risk,
    string? Status,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// A single freight opportunity card. Aggregates the geography, financial ruler,
/// SLA windows, consolidated physical demands and the carrier's own bid context so
/// the board can be scanned at a glance.
/// </summary>
public record FreightOfferListItemDto(
    string Id,
    string Contractor,
    string RouteLabel,
    decimal TargetValue,
    int TotalBids,
    string PickupLabel,
    string EtaLabel,
    string TotalWeight,
    string TotalVolume,
    IReadOnlyList<string> Requirements,
    bool IsExpiringSoon,
    double HoursLeft,
    string? SegmentId,
    string Risk,
    string? BidStatus);

/// <summary>Paged opportunity board response.</summary>
public record PagedFreightOffersDto(
    IReadOnlyList<FreightOfferListItemDto> Items,
    int Page,
    int PageSize,
    int Total);

/// <summary>Full detail for a single freight offer opened from the board.</summary>
public record FreightOfferDetailDto(
    string Id,
    string? SegmentId,
    string Contractor,
    string RouteLabel,
    IReadOnlyList<string> Stops,
    decimal TargetValue,
    int TotalBids,
    IReadOnlyList<string> Requirements,
    string Details);
