using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using System.Security.Claims;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/rotas")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly IRoutePreviewService _routePreviewService;

    public RoutesController(IRoutePreviewService routePreviewService)
    {
        _routePreviewService = routePreviewService;
    }

    private Guid GetContractorId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    /// <summary>
    /// Simulates consolidating the given route segments (Trechos) into a route.
    /// Read-only: no data is written. See POST /api/leiloes to effectively create it.
    /// </summary>
    [HttpPost("preview")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> Preview([FromBody] RoutePreviewRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _routePreviewService.PreviewAsync(GetContractorId(), dto, cancellationToken);
        return Ok(result);
    }
}
