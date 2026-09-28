using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Constants;
using Sigloc.Api.Extensions;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/partnerships")]
[Authorize] // acessível a ambos os lados da parceria; a rede retornada depende do papel
public class PartnersController : ControllerBase
{
    private readonly IPartnerNetworkService _partnerNetworkService;

    public PartnersController(IPartnerNetworkService partnerNetworkService)
    {
        _partnerNetworkService = partnerNetworkService;
    }

    /// <summary>
    /// GET /api/partnerships - rede de parceiros do usuário autenticado.
    /// Contratante recebe a rede de transportadoras; transportadora recebe a rede
    /// de contratantes. O papel é lido do JWT, então um único endpoint atende os dois.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNetwork(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        if (User.IsInRole(Roles.Carrier))
        {
            var carrierNetwork = await _partnerNetworkService.GetCarrierNetworkAsync(companyId, cancellationToken);
            return Ok(carrierNetwork);
        }

        var result = await _partnerNetworkService.GetNetworkAsync(companyId, cancellationToken);
        return Ok(result);
    }
}