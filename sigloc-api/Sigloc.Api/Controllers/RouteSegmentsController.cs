using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/route-segments")]
[Authorize]
public class RouteSegmentsController : ControllerBase
{
    private readonly IRouteSegmentService _routeSegmentService;

    public RouteSegmentsController(IRouteSegmentService routeSegmentService)
    {
        _routeSegmentService = routeSegmentService;
    }

    private Guid GetContractorId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] RouteSegmentRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _routeSegmentService.CreateAsync(GetContractorId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _routeSegmentService.GetByIdAsync(GetContractorId(), id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Search(
        RouteSegmentQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await _routeSegmentService.SearchAsync(GetContractorId(), query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] RouteSegmentRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _routeSegmentService.UpdateAsync(GetContractorId(), id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _routeSegmentService.DeleteAsync(GetContractorId(), id, cancellationToken);
        return NoContent();
    }
}
