using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/consolidated_route")]
[Authorize] 
public class ConsolidatedRouteController : ControllerBase
{
    private readonly IConsolidatedRouteService _consolidatedRouteService;

    public ConsolidatedRouteController(IConsolidatedRouteService consolidatedRouteService)
    {
        _consolidatedRouteService = consolidatedRouteService;
    }

    private Guid GetShipperIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] CreateConsolidatedRouteDto dto, CancellationToken cancellationToken)
    {
        var carrierId = GetShipperIdFromToken();
        var result = await _consolidatedRouteService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _consolidatedRouteService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var carrierId = GetShipperIdFromToken();
        var result = await _consolidatedRouteService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Shipper")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConsolidatedRouteDto dto, CancellationToken cancellationToken)
    {
        await _consolidatedRouteService.UpdateAsync(id, dto, cancellationToken);
        return NoContent(); // 204 No Content is standard for successful updates
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Shipper")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _consolidatedRouteService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
