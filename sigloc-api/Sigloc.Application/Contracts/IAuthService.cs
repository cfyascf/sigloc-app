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

    /// <summary>Open self-service registration of a shipper company using a Google account.</summary>
    Task<AuthResultDto> RegisterContractorWithGoogleAsync(RegisterContractorGoogleDto dto, CancellationToken cancellationToken = default);

    /// <summary>Smart onboarding of a carrier through an invite link using a Google account.</summary>
    Task<AuthResultDto> RegisterCarrierByInviteWithGoogleAsync(string token, RegisterCarrierGoogleDto dto, CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user with e-mail and password and issues a JWT.</summary>
    Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user with a Google account and issues a JWT.</summary>
    Task<AuthResultDto> LoginWithGoogleAsync(GoogleLoginDto dto, CancellationToken cancellationToken = default);
}
