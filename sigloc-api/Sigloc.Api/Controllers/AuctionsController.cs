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

    private Guid GetCarrierId() => User.GetCompanyId();

    [HttpPost]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] CreateAuctionRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _auctionService.CreateAsync(GetContractorId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Auction.Id }, result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Search(
        [FromQuery] AuctionQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await _auctionService.SearchAsync(GetContractorId(), query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _auctionService.GetDetailAsync(GetContractorId(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAuctionRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _auctionService.UpdateAsync(GetContractorId(), id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _auctionService.DeleteAsync(GetContractorId(), id, cancellationToken);
        return NoContent();
    }

    /// <summary>Ranked list of active bids (Motor de Ranking) for the auction.</summary>
    [HttpGet("{id:guid}/bids")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetBidRanking(Guid id, CancellationToken cancellationToken)
    {
        var result = await _auctionService.GetBidRankingAsync(GetContractorId(), id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Awards the auction to the chosen bid (Adjudicação), creating the trip.</summary>
    [HttpPost("{id:guid}/award")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Award(Guid id, [FromBody] AwardAuctionRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _auctionService.AwardAsync(GetContractorId(), id, dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Carrier bid workspace (Workspace de Lance): deep route data, competition and fleet.</summary>
    [HttpGet("{id:guid}/carrier-analysis")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> GetCarrierAnalysis(Guid id, CancellationToken cancellationToken)
    {
        var result = await _auctionService.GetCarrierAnalysisAsync(GetCarrierId(), id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Submits a carrier bid after running the Constraint Engine (Hard Block).</summary>
    [HttpPost("{id:guid}/bids")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> PlaceBid(Guid id, [FromBody] PlaceBidRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _auctionService.PlaceBidAsync(GetCarrierId(), id, dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
