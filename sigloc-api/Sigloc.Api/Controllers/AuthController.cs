using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Open self-service registration of a shipper company (Contratante).</summary>
    [HttpPost("register/contratante")]
    public async Task<IActionResult> RegisterContractor([FromBody] RegisterContractorDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterContractorAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Validates a smart invite token (Convite Inteligente).</summary>
    [HttpGet("invite/{token}")]
    public async Task<IActionResult> ValidateInvite(string token, CancellationToken cancellationToken)
    {
        var result = await _authService.ValidateInviteAsync(token, cancellationToken);
        return Ok(result);
    }

    /// <summary>Smart onboarding of a carrier (Transportadora) through an invite link.</summary>
    [HttpPost("invite/{token}/register")]
    public async Task<IActionResult> RegisterCarrierByInvite(string token, [FromBody] RegisterCarrierDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterCarrierByInviteAsync(token, dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Authenticates a user and issues a JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
    }
}
