using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.DTOs;
using Sigloc.Application.Services;
using Sigloc.Domain.Constants;
using Sigloc.Application.Contracts;
using Sigloc.Api.Extensions;

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

    [HttpPost]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto, CancellationToken cancellationToken)
    {
        var carrierId = User.GetCompanyId();
        var result = await _vehicleService.CreateAsync(carrierId, dto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpGet("{id}")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var carrierId = User.GetCompanyId();
        var result = await _vehicleService.GetByIdAsync(carrierId, id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var carrierId = User.GetCompanyId();
        var result = await _vehicleService.GetAllAsync(carrierId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto dto, CancellationToken cancellationToken)
    {
        var carrierId = User.GetCompanyId();
        await _vehicleService.UpdateAsync(carrierId, id, dto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var carrierId = User.GetCompanyId();
        await _vehicleService.DeleteAsync(carrierId, id, cancellationToken);
        return NoContent();
    }
}
