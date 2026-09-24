using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    private Guid GetCarrierId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Create([FromBody] VehicleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.CreateAsync(GetCarrierId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetByIdAsync(GetCarrierId(), id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Search(
        [FromQuery] VehicleQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await _vehicleService.SearchAsync(GetCarrierId(), query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] VehicleRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.UpdateAsync(GetCarrierId(), id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _vehicleService.DeleteAsync(GetCarrierId(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("validate-plate")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> ValidatePlate([FromQuery] string plate, CancellationToken cancellationToken)
    {
        var available = await _vehicleService.IsPlateAvailableAsync(GetCarrierId(), plate, cancellationToken);
        return Ok(new { available });
    }
}
