using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using Sigloc.Api.Extensions;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/auctions")]
[Authorize]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;

    public AuctionsController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    private Guid GetContractorId() => User.GetCompanyId();

    [HttpPost]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] CreateAuctionRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _auctionService.CreateAsync(GetContractorId(), dto, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Auction.Id }, result);
    }
}
