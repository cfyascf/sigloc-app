namespace Sigloc.Application.DTOs;

/// <summary>
/// The authenticated user's company profile. CNPJ is a legal identifier and is
/// returned read-only; the carrier operational metrics are system-managed.
/// The logistics contact is shipper-only and null for carriers.
/// </summary>
public record CompanyProfileDto(
    Guid Id,
    string Role,
    string Cnpj,
    string CompanyName,
    string? TradeName,
    double? AverageRating,
    bool? HasActiveInsurancePolicy,
    int? OnTimeDeliveryRate,
    string? LogisticsContactName = null,
    string? LogisticsContactPhone = null,
    string? LogisticsContactEmail = null);

/// <summary>
/// Request body for PUT /api/profile. Only the editable fields are accepted.
/// For the logistics contact fields, null keeps the stored value and blank clears it.
/// </summary>
public record UpdateCompanyProfileDto(
    string? CompanyName,
    string? TradeName,
    string? LogisticsContactName = null,
    string? LogisticsContactPhone = null,
    string? LogisticsContactEmail = null);
