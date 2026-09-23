using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/leiloes")]
[Authorize]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;

    public AuctionsController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    private Guid GetContractorId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    /// <summary>
    /// Atomically consolidates the given route segments into a new route and opens an
    /// auction for it. Returns 409 (TRECHO_INDISPONIVEL) if any segment is not Available.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] CreateAuctionRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _auctionService.CreateAsync(GetContractorId(), dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
