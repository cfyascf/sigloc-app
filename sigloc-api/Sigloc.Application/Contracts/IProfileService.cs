using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IProfileService
{
    /// <summary>
    /// Returns the authenticated user's company profile. The <paramref name="isCarrier"/>
    /// flag selects whether the carrier or the contractor record is loaded.
    /// </summary>
    Task<CompanyProfileDto> GetAsync(Guid companyId, bool isCarrier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the editable company data (company name and trade name) for the authenticated
    /// user's company. CNPJ and the operational metrics are not editable.
    /// </summary>
    Task<CompanyProfileDto> UpdateAsync(Guid companyId, bool isCarrier, UpdateCompanyProfileDto dto, CancellationToken cancellationToken = default);
}
