using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IAuthService
{
    /// <summary>Open self-service registration of a shipper company and its initial user.</summary>
    Task<AuthResultDto> RegisterContractorAsync(RegisterContractorDto dto, CancellationToken cancellationToken = default);

    /// <summary>Validates a smart invite token and returns the inviting contractor context.</summary>
    Task<InviteValidationDto> ValidateInviteAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Smart onboarding of a carrier through an invite link.</summary>
    Task<AuthResultDto> RegisterCarrierByInviteAsync(string token, RegisterCarrierDto dto, CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user and issues a JWT.</summary>
    Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}
