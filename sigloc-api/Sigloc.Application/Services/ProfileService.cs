using System.Net.Mail;
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
        var companyName = Validate(dto, isCarrier);
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
        contractor.LogisticsContactName = Patch(dto.LogisticsContactName, contractor.LogisticsContactName);
        contractor.LogisticsContactPhone = Patch(dto.LogisticsContactPhone, contractor.LogisticsContactPhone);
        contractor.LogisticsContactEmail = Patch(dto.LogisticsContactEmail, contractor.LogisticsContactEmail);

        await _contractorRepository.UpdateAsync(contractor, cancellationToken);
        return MapContractor(contractor);
    }

    // Shared with registration (AuthService); long names break cards, headers and notifications.
    internal const int MaxCompanyNameLength = 150;

    private static string Validate(UpdateCompanyProfileDto dto, bool isCarrier)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            errors.Add(new ValidationError("razaoSocial", "Campo obrigatório."));
        }
        else if (dto.CompanyName.Trim().Length > MaxCompanyNameLength)
        {
            errors.Add(new ValidationError("razaoSocial", $"Máximo de {MaxCompanyNameLength} caracteres."));
        }

        if (dto.TradeName?.Trim().Length > MaxCompanyNameLength)
        {
            errors.Add(new ValidationError("nomeFantasia", $"Máximo de {MaxCompanyNameLength} caracteres."));
        }

        // Carriers have no logistics contact; their values are ignored rather than rejected.
        if (!isCarrier)
        {
            ValidateContact(dto, errors);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors, "Não foi possível atualizar os dados da empresa.");
        }

        return dto.CompanyName!.Trim();
    }

    private static void ValidateContact(UpdateCompanyProfileDto dto, List<ValidationError> errors)
    {
        if (dto.LogisticsContactName?.Trim().Length > 120)
        {
            errors.Add(new ValidationError("logisticsContactName", "Máximo de 120 caracteres."));
        }

        if (!string.IsNullOrWhiteSpace(dto.LogisticsContactPhone))
        {
            var phone = dto.LogisticsContactPhone.Trim();
            var digits = phone.Count(char.IsAsciiDigit);
            if (phone.Length > 20 || digits is < 10 or > 13 || phone.Any(c => !char.IsAsciiDigit(c) && !"+()- ".Contains(c)))
            {
                errors.Add(new ValidationError("logisticsContactPhone", "Informe um telefone com DDD (10 a 13 dígitos)."));
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.LogisticsContactEmail))
        {
            var email = dto.LogisticsContactEmail.Trim();
            if (email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !parsed.Host.Contains('.'))
            {
                errors.Add(new ValidationError("logisticsContactEmail", "Informe um e-mail válido."));
            }
        }
    }

    /// <summary>Null keeps the stored value; blank clears it; anything else is trimmed and stored.</summary>
    private static string? Patch(string? incoming, string? current) =>
        incoming == null ? current : string.IsNullOrWhiteSpace(incoming) ? null : incoming.Trim();

    private static CompanyProfileDto MapContractor(Contractor contractor) => new(
        Id: contractor.Id,
        Role: Roles.Shipper,
        Cnpj: contractor.Cnpj,
        CompanyName: contractor.CompanyName,
        TradeName: contractor.TradeName,
        AverageRating: null,
        HasActiveInsurancePolicy: null,
        OnTimeDeliveryRate: null,
        LogisticsContactName: contractor.LogisticsContactName,
        LogisticsContactPhone: contractor.LogisticsContactPhone,
        LogisticsContactEmail: contractor.LogisticsContactEmail);

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
