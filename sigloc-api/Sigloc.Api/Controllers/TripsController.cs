using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Api.Extensions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/viagens")]
[Authorize(Policy = Policies.RequireShipperAccess)]
public sealed class TripsController(ITripMonitoringService service) : ControllerBase
{
    [HttpGet("ativas")]
    public async Task<IActionResult> Search([FromQuery] TripQueryDto query, CancellationToken ct) =>
        Ok(await service.SearchAsync(User.GetCompanyId(), query, ct));

    [HttpGet("{id:guid}/detalhes")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await service.GetDetailAsync(User.GetCompanyId(), id, ct));
    }
}
