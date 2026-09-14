namespace Sigloc.Application.DTOs;

/// <summary>Request body for POST /api/auth/register/contratante.</summary>
public record RegisterContractorDto(
    string? Cnpj,
    string? CompanyName,
    string? TradeName,
    string? Email,
    string? Password);

/// <summary>Request body for POST /api/auth/invite/{token}/register.</summary>
public record RegisterCarrierDto(
    string? Cnpj,
    string? CompanyName,
    string? TradeName,
    string? Email,
    string? Password);

/// <summary>Request body for POST /api/auth/login.</summary>
public record LoginDto(
    string? Email,
    string? Password);

/// <summary>Request body for POST /api/auth/register/administrator/google.</summary>
public record RegisterAdministratorGoogleDto(string? IdToken);

/// <summary>
/// Request body for POST /api/auth/register/contratante/google. Company data is
/// still required; identity (email/name) comes from the verified Google token.
/// </summary>
public record RegisterContractorGoogleDto(
    string? IdToken,
    string? Cnpj,
    string? CompanyName,
    string? TradeName);

/// <summary>
/// Request body for POST /api/auth/invite/{token}/register/google. Company data is
/// still required; identity (email/name) comes from the verified Google token.
/// </summary>
public record RegisterCarrierGoogleDto(
    string? IdToken,
    string? Cnpj,
    string? CompanyName,
    string? TradeName);

/// <summary>Request body for POST /api/auth/login/google.</summary>
public record GoogleLoginDto(string? IdToken);

/// <summary>
/// Request body for POST /api/auth/invite. Issued by an authenticated contractor;
/// the owning company is taken from the JWT, never from the body.
/// </summary>
public record CreateInviteDto(
    string? InviteeEmail,
    int? ExpiresInDays);

/// <summary>Response for POST /api/auth/invite.</summary>
public record InviteCreatedDto(
    string Token,
    string InviteLink,
    DateTimeOffset? ExpiresAt);

/// <summary>Response for GET /api/auth/invite/{token}.</summary>
public record InviteValidationDto(
    bool Valid,
    string? ContractorName,
    string Message);

/// <summary>User payload embedded in registration and login responses.</summary>
public record AuthUserDto(
    Guid Id,
    string Email,
    string ProfileType,
    Guid? CompanyId);

/// <summary>Response for login and registration endpoints that issue a token.</summary>
public record AuthResultDto(
    string Token,
    AuthUserDto User);
