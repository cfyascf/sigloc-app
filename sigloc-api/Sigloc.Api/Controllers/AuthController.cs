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

    /// <summary>Registration of a shipper (Contratante) using a Google account.</summary>
    [HttpPost("register/contratante/google")]
    public async Task<IActionResult> RegisterContractorWithGoogle([FromBody] RegisterContractorGoogleDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterContractorWithGoogleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Smart onboarding of a carrier (Transportadora) via invite using a Google account.</summary>
    [HttpPost("invite/{token}/register/google")]
    public async Task<IActionResult> RegisterCarrierByInviteWithGoogle(string token, [FromBody] RegisterCarrierGoogleDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterCarrierByInviteWithGoogleAsync(token, dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Authenticates a user with e-mail and password and issues a JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>Authenticates a user with a Google account and issues a JWT.</summary>
    [HttpPost("login/google")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginWithGoogleAsync(dto, cancellationToken);
        return Ok(result);
    }
}
