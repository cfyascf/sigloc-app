using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Constants;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IContractorRepository _contractorRepository;
    private readonly ICarrierRepository _carrierRepository;

    public ProfileService(
        IContractorRepository contractorRepository,
        ICarrierRepository carrierRepository)
    {
        _contractorRepository = contractorRepository;
        _carrierRepository = carrierRepository;
    }

    public async Task<CompanyProfileDto> GetAsync(Guid companyId, bool isCarrier, CancellationToken cancellationToken = default)
    {
        if (isCarrier)
        {
            var carrier = await _carrierRepository.GetByIdAsync(companyId, cancellationToken)
                ?? throw new CompanyNotFoundException(companyId);
            return MapCarrier(carrier);
        }

        var contractor = await _contractorRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(companyId);
        return MapContractor(contractor);
    }

    public async Task<CompanyProfileDto> UpdateAsync(Guid companyId, bool isCarrier, UpdateCompanyProfileDto dto, CancellationToken cancellationToken = default)
    {
        var companyName = Validate(dto);
        var tradeName = string.IsNullOrWhiteSpace(dto.TradeName) ? null : dto.TradeName.Trim();

        if (isCarrier)
        {
            var carrier = await _carrierRepository.GetTrackedByIdAsync(companyId, cancellationToken)
                ?? throw new CompanyNotFoundException(companyId);

            carrier.CompanyName = companyName;
            carrier.TradeName = tradeName;

            await _carrierRepository.UpdateAsync(carrier, cancellationToken);
            return MapCarrier(carrier);
        }

        var contractor = await _contractorRepository.GetTrackedByIdAsync(companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(companyId);

        contractor.CompanyName = companyName;
        contractor.TradeName = tradeName;

        await _contractorRepository.UpdateAsync(contractor, cancellationToken);
        return MapContractor(contractor);
    }

    private static string Validate(UpdateCompanyProfileDto dto)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            errors.Add(new ValidationError("razaoSocial", "Campo obrigatório."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors, "Não foi possível atualizar os dados da empresa.");
        }

        return dto.CompanyName!.Trim();
    }

    private static CompanyProfileDto MapContractor(Contractor contractor) => new(
        Id: contractor.Id,
        Role: Roles.Shipper,
        Cnpj: contractor.Cnpj,
        CompanyName: contractor.CompanyName,
        TradeName: contractor.TradeName,
        AverageRating: null,
        HasActiveInsurancePolicy: null,
        OnTimeDeliveryRate: null);

    private static CompanyProfileDto MapCarrier(Carrier carrier) => new(
        Id: carrier.Id,
        Role: Roles.Carrier,
        Cnpj: carrier.Cnpj,
        CompanyName: carrier.CompanyName,
        TradeName: carrier.TradeName,
        AverageRating: carrier.AverageRating,
        HasActiveInsurancePolicy: carrier.HasActiveInsurancePolicy,
        OnTimeDeliveryRate: carrier.OnTimeDeliveryRate);
}
