using System.Text.Json.Serialization;

namespace Sigloc.Application.DTOs;

/// <summary>Request body for POST /api/auth/register/contratante.</summary>
public record RegisterContractorDto(
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("razaoSocial")] string? CompanyName,
    [property: JsonPropertyName("nomeFantasia")] string? TradeName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("senha")] string? Password);

/// <summary>Request body for POST /api/auth/invite/{token}/register.</summary>
public record RegisterCarrierDto(
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("razaoSocial")] string? CompanyName,
    [property: JsonPropertyName("nomeFantasia")] string? TradeName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("senha")] string? Password);

/// <summary>Request body for POST /api/auth/login.</summary>
public record LoginDto(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("senha")] string? Password);

/// <summary>
/// Request body for POST /api/auth/register/contratante/google. Company data is
/// still required; identity (email/name) comes from the verified Google token.
/// </summary>
public record RegisterContractorGoogleDto(
    [property: JsonPropertyName("idToken")] string? IdToken,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("razaoSocial")] string? CompanyName,
    [property: JsonPropertyName("nomeFantasia")] string? TradeName);

/// <summary>
/// Request body for POST /api/auth/invite/{token}/register/google. Company data is
/// still required; identity (email/name) comes from the verified Google token.
/// </summary>
public record RegisterCarrierGoogleDto(
    [property: JsonPropertyName("idToken")] string? IdToken,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("razaoSocial")] string? CompanyName,
    [property: JsonPropertyName("nomeFantasia")] string? TradeName);

/// <summary>Request body for POST /api/auth/login/google.</summary>
public record GoogleLoginDto(
    [property: JsonPropertyName("idToken")] string? IdToken);

/// <summary>
/// Request body for POST /api/auth/invite. Issued by an authenticated contractor;
/// the owning company is taken from the JWT, never from the body.
/// </summary>
public record CreateInviteDto(
    [property: JsonPropertyName("emailConvidado")] string? InviteeEmail,
    [property: JsonPropertyName("expiraEmDias")] int? ExpiresInDays);

/// <summary>Response for POST /api/auth/invite.</summary>
public record InviteCreatedDto(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("linkConvite")] string InviteLink,
    [property: JsonPropertyName("expiraEm")] DateTimeOffset? ExpiresAt);

/// <summary>Response for GET /api/auth/invite/{token}.</summary>
public record InviteValidationDto(
    [property: JsonPropertyName("valido")] bool Valid,
    [property: JsonPropertyName("contratanteNome")] string? ContractorName,
    [property: JsonPropertyName("mensagem")] string Message);

/// <summary>User payload embedded in registration and login responses.</summary>
public record AuthUserDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("tipoPerfil")] string ProfileType,
    [property: JsonPropertyName("empresaId")] Guid? CompanyId);

/// <summary>Response for login and registration endpoints that issue a token.</summary>
public record AuthResultDto(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("usuario")] AuthUserDto User);
