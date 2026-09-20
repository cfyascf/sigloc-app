using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Constants;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/parcerias")]
[Authorize(Policy = Policies.RequireShipperAccess)] // história é do Operador Logístico (Contratante)
public class PartnersController : ControllerBase
{
    private readonly IPartnerNetworkService _partnerNetworkService;

    public PartnersController(IPartnerNetworkService partnerNetworkService)
    {
        _partnerNetworkService = partnerNetworkService;
    }

    /// <summary>GET /api/parcerias - listagem analítica da rede de transportadoras parceiras.</summary>
    [HttpGet]
    public async Task<IActionResult> GetNetwork(CancellationToken cancellationToken)
    {
        var contractorId = GetCompanyId();
        var result = await _partnerNetworkService.GetNetworkAsync(contractorId, cancellationToken);
        return Ok(result);
    }

    // Duplicado do AuthController de propósito: aquele método é privado, e não queria
    // tocar num arquivo que já está em outro PR aberto (feature/convite-parceria-ativo).
    // Depois que os dois PRs fecharem, vale extrair isso pra um método de extensão
    // compartilhado (ex: User.GetCompanyId()) e remover a duplicação.
    private Guid GetCompanyId()
    {
        var raw = User.FindFirstValue("empresaId");
        if (Guid.TryParse(raw, out var companyId))
        {
            return companyId;
        }

        throw new UnauthorizedAccessException("O token não contém uma empresa (empresaId) válida.");
    }
}