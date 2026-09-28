using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Constants;
using Sigloc.Api.Extensions;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/partnerships")]
[Authorize(Policy = Policies.RequireShipperAccess)] // história é do Operador Logístico (Contratante)
public class PartnersController : ControllerBase
{
    private readonly IPartnerNetworkService _partnerNetworkService;

    public PartnersController(IPartnerNetworkService partnerNetworkService)
    {
        _partnerNetworkService = partnerNetworkService;
    }

    /// <summary>GET /api/partnerships - listagem analítica da rede de transportadoras parceiras.</summary>
    [HttpGet]
    public async Task<IActionResult> GetNetwork(CancellationToken cancellationToken)
    {
        var contractorId = User.GetCompanyId();
        var result = await _partnerNetworkService.GetNetworkAsync(contractorId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET /api/partnerships/carrier - rede de contratantes conectados à transportadora autenticada.</summary>
    [HttpGet("carrier")]
    [Authorize(Policy = Policies.RequireCarrierAccess)]
    public async Task<IActionResult> GetCarrierNetwork(CancellationToken cancellationToken)
    {
        var carrierId = User.GetCompanyId();
        var result = await _partnerNetworkService.GetCarrierNetworkAsync(carrierId, cancellationToken);
        return Ok(result);
    }
}