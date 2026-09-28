using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

/// <summary>
/// Carrier-facing opportunity board (Mural de Fretes). Resolves the B2B partnership
/// filter, the competitive ruler, the urgency engine and the consolidated physical
/// demands so the front-end can render offer cards without further computation.
/// </summary>
public interface IFreightOfferService
{
    /// <summary>Paged list of open auctions available to the carrier's active partners.</summary>
    Task<PagedFreightOffersDto> ListAsync(
        Guid carrierId,
        FreightOfferQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>Full detail of a single available offer, or throws when it is not visible.</summary>
    Task<FreightOfferDetailDto> GetByIdAsync(
        Guid carrierId,
        Guid offerId,
        CancellationToken cancellationToken = default);
}
