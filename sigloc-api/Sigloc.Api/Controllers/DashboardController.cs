using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Api.Extensions;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Constants;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private Guid GetContractorId() => User.GetCompanyId();

    private Guid GetCarrierId() => User.GetCompanyId();

    /// <summary>Aggregated executive dashboard (Dashboard Executivo) for the contractor.</summary>
    [HttpGet("executivo")]
    [Authorize(Policy = Policies.RequireShipperAccess)]
    public async Task<IActionResult> GetExecutive(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetExecutiveDashboardAsync(GetContractorId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Aggregated operational dashboard (Dashboard do Transportador) for the carrier.</summary>
    [HttpGet("transportador")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> GetCarrier(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetCarrierDashboardAsync(GetCarrierId(), cancellationToken);
        return Ok(result);
    }
}
