using System.Text.RegularExpressions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public partial class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IContractorRepository _contractors;
    private readonly ICarrierRepository _carriers;
    private readonly IPartnershipInviteRepository _invites;
    private readonly IPartnerConnectionRepository _connections;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;

    public AuthService(
        IUserRepository users,
        IContractorRepository contractors,
        ICarrierRepository carriers,
        IPartnershipInviteRepository invites,
        IPartnerConnectionRepository connections,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider)
    {
        _users = users;
        _contractors = contractors;
        _carriers = carriers;
        _invites = invites;
        _connections = connections;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    public async Task<AuthResultDto> RegisterContractorAsync(RegisterContractorDto dto, CancellationToken cancellationToken = default)
    {
        var cnpj = NormalizeCnpj(dto.Cnpj);
        var email = NormalizeEmail(dto.Email);

        ValidateRegistration(cnpj, dto.CompanyName, email, dto.Password);

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new EmailAlreadyExistsException(email);
        }

        if (await _contractors.CnpjExistsAsync(cnpj, cancellationToken))
        {
            throw new CnpjAlreadyExistsException(cnpj);
        }

        var contractor = new Contractor
        {
            Id = Guid.NewGuid(),
            Cnpj = cnpj,
            CompanyName = dto.CompanyName!.Trim(),
            TradeName = string.IsNullOrWhiteSpace(dto.TradeName) ? null : dto.TradeName.Trim()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(dto.Password!),
            ProfileType = ProfileType.Contratante,
            CompanyId = contractor.Id
        };

        // Company and user are persisted atomically in a single unit of work.
        await _contractors.AddAsync(contractor, cancellationToken);
        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildAuthResult(user);
    }

    public async Task<InviteValidationDto> ValidateInviteAsync(string token, CancellationToken cancellationToken = default)
    {
        var invite = await _invites.GetByTokenAsync(token, cancellationToken);

        if (invite is null || invite.IsUsed || IsExpired(invite))
        {
            return new InviteValidationDto(false, null, "O convite é inválido ou expirou.");
        }

        var contractor = await _contractors.GetByIdAsync(invite.ContractorId, cancellationToken);
        if (contractor is null)
        {
            return new InviteValidationDto(false, null, "O convite é inválido ou expirou.");
        }

        var name = contractor.TradeName ?? contractor.CompanyName;
        return new InviteValidationDto(
            true,
            name,
            $"A Empresa {name} deseja estabelecer uma parceria com você.");
    }

    public async Task<AuthResultDto> RegisterCarrierByInviteAsync(string token, RegisterCarrierDto dto, CancellationToken cancellationToken = default)
    {
        var invite = await _invites.GetByTokenAsync(token, cancellationToken);
        if (invite is null || invite.IsUsed || IsExpired(invite))
        {
            throw new InvalidInviteException();
        }

        var contractor = await _contractors.GetByIdAsync(invite.ContractorId, cancellationToken)
            ?? throw new InvalidInviteException();

        var cnpj = NormalizeCnpj(dto.Cnpj);

        // Cenário B: the carrier already exists. Abort the registration and tell the
        // front-end to redirect to login so the carrier can accept the pending
        // partnership after authenticating.
        if (await _carriers.CnpjExistsAsync(cnpj, cancellationToken))
        {
            throw new CarrierAlreadyRegisteredException(cnpj);
        }

        // Cenário A: brand-new carrier. Full data is required.
        var email = NormalizeEmail(dto.Email);
        ValidateRegistration(cnpj, dto.CompanyName, email, dto.Password);

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var carrier = new Carrier
        {
            Id = Guid.NewGuid(),
            Cnpj = cnpj,
            CompanyName = dto.CompanyName!.Trim(),
            TradeName = string.IsNullOrWhiteSpace(dto.TradeName) ? null : dto.TradeName.Trim()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(dto.Password!),
            ProfileType = ProfileType.Transportador,
            CompanyId = carrier.Id
        };

        var connection = new PartnerConnection
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            CarrierId = carrier.Id,
            Status = PartnershipStatus.Active
        };

        invite.IsUsed = true;

        // Carrier, user and partnership connection are persisted atomically.
        await _carriers.AddAsync(carrier, cancellationToken);
        await _users.AddAsync(user, cancellationToken);
        await _connections.AddAsync(connection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildAuthResult(user);
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(dto.Email);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new InvalidCredentialsException();
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return BuildAuthResult(user);
    }

    private AuthResultDto BuildAuthResult(User user)
    {
        var token = _jwtProvider.Generate(
            user.Id,
            user.Email,
            user.ProfileType.ToRole(),
            user.ProfileType.ToWire(),
            user.CompanyId);

        var userDto = new AuthUserDto(
            user.Id,
            user.Email,
            user.ProfileType.ToWire(),
            user.CompanyId);

        return new AuthResultDto(token, userDto);
    }

    private static bool IsExpired(PartnershipInvite invite)
        => invite.ExpiresAt.HasValue && invite.ExpiresAt.Value <= DateTimeOffset.UtcNow;

    private static string NormalizeEmail(string? email)
        => email?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizeCnpj(string? cnpj)
        => cnpj is null ? string.Empty : DigitsOnlyRegex().Replace(cnpj, string.Empty);

    private static void ValidateRegistration(string cnpj, string? companyName, string email, string? password)
    {
        var errors = new List<ValidationError>();

        if (cnpj.Length != 14)
        {
            errors.Add(new ValidationError("cnpj", "CNPJ inválido. Informe os 14 dígitos."));
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            errors.Add(new ValidationError("razaoSocial", "Campo obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex().IsMatch(email))
        {
            errors.Add(new ValidationError("email", "E-mail inválido."));
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errors.Add(new ValidationError("senha", "A senha deve ter ao menos 8 caracteres."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors, "Não foi possível concluir o registro.");
        }
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnlyRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
