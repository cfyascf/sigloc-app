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
        try
            {
                var carrierId = GetCarrierIdFromToken();
                var result = await _vehicleService.CreateAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                // Captura erros de validação (ex: eixo <= 0, placa fora do formato)
                return BadRequest(new { erro = ex.Message }); // HTTP 400
            }
            catch (InvalidOperationException ex)
            {
                // Captura erros de regra de negócio (ex: placa duplicada no banco)
                return Conflict(new { erro = ex.Message }); // HTTP 409
            }
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
        
        try
        {
            await _vehicleService.UpdateAsync(id, dto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { erro = ex.Message }); // 404
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message }); // 400 Bad Request
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { erro = ex.Message }); // 409 Conflict
        }
    
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
