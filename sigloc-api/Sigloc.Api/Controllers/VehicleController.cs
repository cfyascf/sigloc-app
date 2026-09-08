using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.DTOs;
using Sigloc.Application.Services;
using Sigloc.Domain.Constants;
using Sigloc.Application.Contracts;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
//[Authorize] 
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    private Guid GetCarrierIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost]
    //[Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto, CancellationToken cancellationToken)
    {
        var carrierId = GetCarrierIdFromToken();
        var result = await _vehicleService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    //[Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    //[Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var carrierId = GetCarrierIdFromToken();
        var result = await _vehicleService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    //[Authorize(Roles = "Carrier")]
    //[Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto dto, CancellationToken cancellationToken)
    {
        await _vehicleService.UpdateAsync(id, dto, cancellationToken);
        return NoContent(); // 204 No Content is standard for successful updates
    }

    [HttpDelete("{id}")]
    //[Authorize(Roles = "Carrier")]
    //[Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _vehicleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
