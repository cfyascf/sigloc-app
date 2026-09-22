namespace Sigloc.Application.DTOs;
using System.Text.Json.Serialization;

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

/// <summary>Resposta para o GET /api/auth/invite/active. ExpiresAt/HoursRemaining vêm null quando o convite foi criado sem expiração (Invite.ExpiresAt == null). Os nomes de propriedade em C# seguem o padrão do arquivo (inglês), mas o JSON exposto usa os nomes em português exigidos pela spec do front (JsonPropertyName)</summary>
public record ActiveInviteDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("codigo")] string Code,
    [property: JsonPropertyName("linkCompleto")] string InviteLink,
    [property: JsonPropertyName("expiraEm")] DateTimeOffset? ExpiresAt,
    [property: JsonPropertyName("horasRestantes")] double? HoursRemaining);

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
