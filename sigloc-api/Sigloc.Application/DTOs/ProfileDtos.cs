namespace Sigloc.Application.DTOs;

/// <summary>
/// The authenticated user's company profile. CNPJ is a legal identifier and is
/// returned read-only; the carrier operational metrics are system-managed.
/// </summary>
public record CompanyProfileDto(
    Guid Id,
    string Role,
    string Cnpj,
    string CompanyName,
    string? TradeName,
    double? AverageRating,
    bool? HasActiveInsurancePolicy,
    int? OnTimeDeliveryRate);

/// <summary>Request body for PUT /api/profile. Only the editable fields are accepted.</summary>
public record UpdateCompanyProfileDto(
    string? CompanyName,
    string? TradeName);
