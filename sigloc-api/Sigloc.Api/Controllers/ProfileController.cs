using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using Sigloc.Api.Extensions;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize] // acessível a ambos os papéis; a empresa retornada depende do papel do JWT
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>GET /api/profile - dados da empresa do usuário autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetAsync(User.GetCompanyId(), User.IsInRole(Roles.Carrier), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT /api/profile - atualiza os dados editáveis da empresa (razão social e nome fantasia).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyProfileDto dto, CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateAsync(User.GetCompanyId(), User.IsInRole(Roles.Carrier), dto, cancellationToken);
        return Ok(result);
    }
}
