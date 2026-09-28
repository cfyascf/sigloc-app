using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Api.Extensions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Api.Controllers;

/// <summary>
/// Opportunity board (Mural de Fretes) for carriers (Dono de Frota). Every action is
/// scoped to the logged carrier's active partnerships.
/// </summary>
[ApiController]
[Route("api/freight-offers")]
[Authorize(Policy = Policies.RequireCarrierAccess)]
public class FreightOffersController : ControllerBase
{
    private readonly IFreightOfferService _freightOfferService;

    public FreightOffersController(IFreightOfferService freightOfferService)
    {
        _freightOfferService = freightOfferService;
    }

    private Guid GetCarrierId() => User.GetCompanyId();

    /// <summary>Paged opportunity board of open auctions from the carrier's active partners.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] FreightOfferQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await _freightOfferService.ListAsync(GetCarrierId(), query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Full detail for a single freight offer opened from the board.</summary>
    [HttpGet("{offerId:guid}")]
    public async Task<IActionResult> GetById(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await _freightOfferService.GetByIdAsync(GetCarrierId(), offerId, cancellationToken);
        return Ok(result);
    }
}
