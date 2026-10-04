using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Unit;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IContractorRepository _contractors = Substitute.For<IContractorRepository>();
    private readonly ICarrierRepository _carriers = Substitute.For<ICarrierRepository>();
    private readonly IPartnershipInviteRepository _invites = Substitute.For<IPartnershipInviteRepository>();
    private readonly IPartnerConnectionRepository _connections = Substitute.For<IPartnerConnectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly IGoogleTokenVerifier _googleTokenVerifier = Substitute.For<IGoogleTokenVerifier>();
    private readonly IInviteLinkBuilder _inviteLinkBuilder = Substitute.For<IInviteLinkBuilder>();
    private readonly IPasswordResetTokenRepository _passwordResetTokens = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IPasswordResetLinkBuilder _passwordResetLinkBuilder = Substitute.For<IPasswordResetLinkBuilder>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private readonly AuthService _service;

    private const string ValidCnpj = "11.222.333/0001-81";
    private const string ValidCnpjDigits = "11222333000181";

    public AuthServiceTests()
    {
        _service = new AuthService(
            _users,
            _contractors,
            _carriers,
            _invites,
            _connections,
            _unitOfWork,
            _passwordHasher,
            _jwtProvider,
            _googleTokenVerifier,
            _inviteLinkBuilder,
            _passwordResetTokens,
            _passwordResetLinkBuilder,
            _emailSender);

        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _jwtProvider.Generate(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns("jwt-token");
        _inviteLinkBuilder.Build(Arg.Any<string>()).Returns(ci => $"https://app/invite/{ci.Arg<string>()}");
        _passwordResetLinkBuilder.TokenExpiryMinutes.Returns(30);
        _passwordResetLinkBuilder.Build(Arg.Any<string>()).Returns(ci => $"https://app/reset/{ci.Arg<string>()}");
    }

    private void SetupGoogle(string email = "google@example.com", string subject = "google-sub", bool emailVerified = true)
        => _googleTokenVerifier.VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo(subject, email, emailVerified, "Google User"));

    // ---------------------------------------------------------------------
    // RegisterContractorAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RegisterContractorAsync_WithValidData_PersistsAndReturnsAuthResult()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _contractors.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterContractorDto(ValidCnpj, "Empresa Ltda", "Empresa", "Owner@Example.com", "password123");

        var result = await _service.RegisterContractorAsync(dto);

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be("owner@example.com");
        result.User.ProfileType.Should().Be("CONTRATANTE");

        await _contractors.Received(1).AddAsync(
            Arg.Is<Contractor>(c => c.Cnpj == ValidCnpjDigits && c.CompanyName == "Empresa Ltda" && c.TradeName == "Empresa"),
            Arg.Any<CancellationToken>());
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "owner@example.com"
                && u.PasswordHash == "hashed-password"
                && u.AuthProvider == AuthProvider.Local
                && u.ProfileType == ProfileType.Contratante),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterContractorAsync_WhenEmailExists_ThrowsEmailAlreadyExists()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterContractorDto(ValidCnpj, "Empresa Ltda", null, "owner@example.com", "password123");

        var act = () => _service.RegisterContractorAsync(dto);

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterContractorAsync_WhenCnpjExists_ThrowsCnpjAlreadyExists()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _contractors.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterContractorDto(ValidCnpj, "Empresa Ltda", null, "owner@example.com", "password123");

        var act = () => _service.RegisterContractorAsync(dto);

        await act.Should().ThrowAsync<CnpjAlreadyExistsException>();
        await _contractors.DidNotReceive().AddAsync(Arg.Any<Contractor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterContractorAsync_WithInvalidData_ThrowsValidation()
    {
        var dto = new RegisterContractorDto("123", "", null, "bad-email", "short");

        var act = () => _service.RegisterContractorAsync(dto);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "cnpj");
        ex.Which.Errors.Should().Contain(e => e.Field == "razaoSocial");
        ex.Which.Errors.Should().Contain(e => e.Field == "email");
        ex.Which.Errors.Should().Contain(e => e.Field == "senha");
    }

    // ---------------------------------------------------------------------
    // CreateInviteAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateInviteAsync_WithValidContractor_CreatesInviteAndReturnsLink()
    {
        var contractorId = Guid.NewGuid();
        _contractors.GetByIdAsync(contractorId, Arg.Any<CancellationToken>())
            .Returns(TestData.Contractor(id: contractorId));

        var dto = new CreateInviteDto("partner@example.com", 5);

        var result = await _service.CreateInviteAsync(contractorId, dto);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.InviteLink.Should().Be($"https://app/invite/{result.Token}");
        result.ExpiresAt.Should().NotBeNull();

        await _invites.Received(1).AddAsync(
            Arg.Is<PartnershipInvite>(i => i.ContractorId == contractorId
                && i.InviteeEmail == "partner@example.com"
                && !i.IsUsed),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInviteAsync_WithZeroExpiry_CreatesNonExpiringInvite()
    {
        var contractorId = Guid.NewGuid();
        _contractors.GetByIdAsync(contractorId, Arg.Any<CancellationToken>())
            .Returns(TestData.Contractor(id: contractorId));

        var dto = new CreateInviteDto(null, 0);

        var result = await _service.CreateInviteAsync(contractorId, dto);

        result.ExpiresAt.Should().BeNull();
        await _invites.Received(1).AddAsync(
            Arg.Is<PartnershipInvite>(i => i.ExpiresAt == null && i.InviteeEmail == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInviteAsync_WhenContractorMissing_ThrowsContractorNotFound()
    {
        var contractorId = Guid.NewGuid();
        _contractors.GetByIdAsync(contractorId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var act = () => _service.CreateInviteAsync(contractorId, new CreateInviteDto(null, null));

        var ex = await act.Should().ThrowAsync<ContractorNotFoundException>();
        ex.Which.Message.Should().Contain(contractorId.ToString());
    }

    // ---------------------------------------------------------------------
    // ValidateInviteAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ValidateInviteAsync_WithValidInvite_ReturnsValidResult()
    {
        var contractor = TestData.Contractor(companyName: "ACME");
        contractor.TradeName = "ACME Trade";
        var invite = new PartnershipInvite
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = contractor.Id,
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.ValidateInviteAsync("tok");

        result.Valid.Should().BeTrue();
        result.ContractorName.Should().Be("ACME Trade");
        result.Message.Should().Contain("ACME Trade");
    }

    [Fact]
    public async Task ValidateInviteAsync_WhenInviteMissing_ReturnsInvalid()
    {
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns((PartnershipInvite?)null);

        var result = await _service.ValidateInviteAsync("tok");

        result.Valid.Should().BeFalse();
        result.ContractorName.Should().BeNull();
    }

    [Fact]
    public async Task ValidateInviteAsync_WhenInviteUsed_ReturnsInvalid()
    {
        var invite = new PartnershipInvite { Id = Guid.NewGuid(), Token = "tok", ContractorId = Guid.NewGuid(), IsUsed = true };
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);

        var result = await _service.ValidateInviteAsync("tok");

        result.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateInviteAsync_WhenInviteExpired_ReturnsInvalid()
    {
        var invite = new PartnershipInvite
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = Guid.NewGuid(),
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);

        var result = await _service.ValidateInviteAsync("tok");

        result.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateInviteAsync_WhenContractorMissing_ReturnsInvalid()
    {
        var invite = new PartnershipInvite
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = Guid.NewGuid(),
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(invite.ContractorId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var result = await _service.ValidateInviteAsync("tok");

        result.Valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateInviteAsync_FallsBackToCompanyName_WhenNoTradeName()
    {
        var contractor = TestData.Contractor(companyName: "Only Company");
        var invite = new PartnershipInvite
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = contractor.Id,
            IsUsed = false,
            ExpiresAt = null
        };
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);

        var result = await _service.ValidateInviteAsync("tok");

        result.Valid.Should().BeTrue();
        result.ContractorName.Should().Be("Only Company");
    }

    // ---------------------------------------------------------------------
    // GetActiveInviteAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetActiveInviteAsync_WhenActiveInviteExists_ReturnsDto()
    {
        var contractorId = Guid.NewGuid();
        var invite = new PartnershipInvite
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = contractorId,
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(10)
        };
        _invites.GetLatestActiveByContractorAsync(contractorId, Arg.Any<CancellationToken>()).Returns(invite);

        var result = await _service.GetActiveInviteAsync(contractorId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(invite.Id);
        result.Code.Should().Be("tok");
        result.InviteLink.Should().Be("https://app/invite/tok");
        result.HoursRemaining.Should().BeApproximately(10, 0.1);
    }

    [Fact]
    public async Task GetActiveInviteAsync_WhenNoExpiry_HoursRemainingIsNull()
    {
        var contractorId = Guid.NewGuid();
        var invite = new PartnershipInvite
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = contractorId,
            IsUsed = false,
            ExpiresAt = null
        };
        _invites.GetLatestActiveByContractorAsync(contractorId, Arg.Any<CancellationToken>()).Returns(invite);

        var result = await _service.GetActiveInviteAsync(contractorId);

        result.Should().NotBeNull();
        result!.ExpiresAt.Should().BeNull();
        result.HoursRemaining.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveInviteAsync_WhenNoInvite_ReturnsNull()
    {
        _invites.GetLatestActiveByContractorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PartnershipInvite?)null);

        var result = await _service.GetActiveInviteAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------------
    // RegisterCarrierByInviteAsync
    // ---------------------------------------------------------------------

    private PartnershipInvite ValidInvite(Guid contractorId)
        => new()
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            ContractorId = contractorId,
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

    [Fact]
    public async Task RegisterCarrierByInviteAsync_WithNewCarrier_PersistsCarrierUserAndConnection()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterCarrierDto(ValidCnpj, "Transp Ltda", "Transp", "carrier@example.com", "password123");

        var result = await _service.RegisterCarrierByInviteAsync("tok", dto);

        result.Token.Should().Be("jwt-token");
        result.User.ProfileType.Should().Be("TRANSPORTADOR");
        invite.IsUsed.Should().BeTrue();

        await _carriers.Received(1).AddAsync(Arg.Is<Carrier>(c => c.Cnpj == ValidCnpjDigits), Arg.Any<CancellationToken>());
        await _users.Received(1).AddAsync(Arg.Is<User>(u => u.ProfileType == ProfileType.Transportador), Arg.Any<CancellationToken>());
        await _connections.Received(1).AddAsync(
            Arg.Is<PartnerConnection>(c => c.ContractorId == contractor.Id
                && c.Status == PartnershipStatus.Active
                && c.InitiatedBy == PartnershipInitiator.Contractor),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCarrierByInviteAsync_WhenInviteInvalid_ThrowsInvalidInvite()
    {
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns((PartnershipInvite?)null);

        var dto = new RegisterCarrierDto(ValidCnpj, "Transp Ltda", null, "carrier@example.com", "password123");

        var act = () => _service.RegisterCarrierByInviteAsync("tok", dto);

        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteAsync_WhenContractorMissing_ThrowsInvalidInvite()
    {
        var invite = ValidInvite(Guid.NewGuid());
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(invite.ContractorId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var dto = new RegisterCarrierDto(ValidCnpj, "Transp Ltda", null, "carrier@example.com", "password123");

        var act = () => _service.RegisterCarrierByInviteAsync("tok", dto);

        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteAsync_WhenCarrierAlreadyExists_ThrowsCarrierAlreadyRegistered()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterCarrierDto(ValidCnpj, "Transp Ltda", null, "carrier@example.com", "password123");

        var act = () => _service.RegisterCarrierByInviteAsync("tok", dto);

        await act.Should().ThrowAsync<CarrierAlreadyRegisteredException>();
        await _carriers.DidNotReceive().AddAsync(Arg.Any<Carrier>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCarrierByInviteAsync_WhenEmailExists_ThrowsEmailAlreadyExists()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterCarrierDto(ValidCnpj, "Transp Ltda", null, "carrier@example.com", "password123");

        var act = () => _service.RegisterCarrierByInviteAsync("tok", dto);

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteAsync_WithInvalidData_ThrowsValidation()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterCarrierDto(ValidCnpj, "", null, "bad", "short");

        var act = () => _service.RegisterCarrierByInviteAsync("tok", dto);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------------------------------------------------------------------
    // ConnectCarrierByInviteAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ConnectCarrierByInviteAsync_WithValidInvite_CreatesConnection()
    {
        var contractor = TestData.Contractor(companyName: "ACME");
        var invite = ValidInvite(contractor.Id);
        var carrierId = Guid.NewGuid();
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _connections.ExistsAsync(contractor.Id, carrierId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ConnectCarrierByInviteAsync(carrierId, "tok");

        result.ContractorName.Should().Be("ACME");
        result.PartnershipStatus.Should().Be("ATIVA");
        invite.IsUsed.Should().BeTrue();

        await _connections.Received(1).AddAsync(
            Arg.Is<PartnerConnection>(c => c.CarrierId == carrierId
                && c.ContractorId == contractor.Id
                && c.InitiatedBy == PartnershipInitiator.Carrier),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConnectCarrierByInviteAsync_WhenInviteInvalid_ThrowsInvalidInvite()
    {
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns((PartnershipInvite?)null);

        var act = () => _service.ConnectCarrierByInviteAsync(Guid.NewGuid(), "tok");

        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Fact]
    public async Task ConnectCarrierByInviteAsync_WhenContractorMissing_ThrowsInvalidInvite()
    {
        var invite = ValidInvite(Guid.NewGuid());
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(invite.ContractorId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var act = () => _service.ConnectCarrierByInviteAsync(Guid.NewGuid(), "tok");

        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Fact]
    public async Task ConnectCarrierByInviteAsync_WhenPartnershipExists_ThrowsPartnershipAlreadyExists()
    {
        var contractor = TestData.Contractor(companyName: "ACME");
        var invite = ValidInvite(contractor.Id);
        var carrierId = Guid.NewGuid();
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        _connections.ExistsAsync(contractor.Id, carrierId, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _service.ConnectCarrierByInviteAsync(carrierId, "tok");

        await act.Should().ThrowAsync<PartnershipAlreadyExistsException>();
        await _connections.DidNotReceive().AddAsync(Arg.Any<PartnerConnection>(), Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------------
    // LoginAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResult()
    {
        var user = TestData.User(email: "user@example.com", passwordHash: "stored-hash", provider: AuthProvider.Local);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("password123", "stored-hash").Returns(true);

        var result = await _service.LoginAsync(new LoginDto("User@Example.com", "password123"));

        result.Token.Should().Be("jwt-token");
        result.User.Id.Should().Be(user.Id);
        _passwordHasher.Received(1).Verify("password123", "stored-hash");
    }

    [Fact]
    public async Task LoginAsync_WithEmptyEmail_ThrowsInvalidCredentials()
    {
        var act = () => _service.LoginAsync(new LoginDto("", "password123"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        await _users.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ThrowsInvalidCredentials()
    {
        var act = () => _service.LoginAsync(new LoginDto("user@example.com", ""));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsInvalidCredentials()
    {
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _service.LoginAsync(new LoginDto("user@example.com", "password123"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsGoogle_ThrowsInvalidCredentials()
    {
        var user = TestData.User(email: "user@example.com", passwordHash: null, provider: AuthProvider.Google);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _service.LoginAsync(new LoginDto("user@example.com", "password123"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordWrong_ThrowsInvalidCredentials()
    {
        var user = TestData.User(email: "user@example.com", passwordHash: "stored-hash", provider: AuthProvider.Local);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", "stored-hash").Returns(false);

        var act = () => _service.LoginAsync(new LoginDto("user@example.com", "wrong"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ---------------------------------------------------------------------
    // RequestPasswordResetAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RequestPasswordResetAsync_WithLocalUser_CreatesTokenAndSendsEmail()
    {
        var user = TestData.User(email: "user@example.com", passwordHash: "hash", provider: AuthProvider.Local);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);

        await _service.RequestPasswordResetAsync(new PasswordResetRequestDto("User@Example.com"));

        await _passwordResetTokens.Received(1).InvalidateActiveForUserAsync(user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _passwordResetTokens.Received(1).AddAsync(Arg.Is<PasswordResetToken>(t => t.UserId == user.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendPasswordResetAsync(user.Email, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenUserMissing_DoesNothing()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await _service.RequestPasswordResetAsync(new PasswordResetRequestDto("unknown@example.com"));

        await _passwordResetTokens.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendPasswordResetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenUserIsGoogle_DoesNothing()
    {
        var user = TestData.User(email: "user@example.com", passwordHash: null, provider: AuthProvider.Google);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);

        await _service.RequestPasswordResetAsync(new PasswordResetRequestDto("user@example.com"));

        await _passwordResetTokens.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenEmailSenderThrows_Swallows()
    {
        var user = TestData.User(email: "user@example.com", passwordHash: "hash", provider: AuthProvider.Local);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _emailSender.SendPasswordResetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("smtp down")));

        var act = () => _service.RequestPasswordResetAsync(new PasswordResetRequestDto("user@example.com"));

        await act.Should().NotThrowAsync();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------------
    // ConfirmPasswordResetAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ConfirmPasswordResetAsync_WithValidToken_UpdatesPassword()
    {
        var user = TestData.User(provider: AuthProvider.Local, passwordHash: "old");
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _passwordResetTokens.GetValidByHashAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(resetToken);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _service.ConfirmPasswordResetAsync(new PasswordResetConfirmDto("rawtoken", "newpassword123"));

        user.PasswordHash.Should().Be("hashed-password");
        resetToken.UsedAt.Should().NotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WithShortPassword_ThrowsValidation()
    {
        var act = () => _service.ConfirmPasswordResetAsync(new PasswordResetConfirmDto("rawtoken", "short"));

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "password");
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WithEmptyToken_ThrowsValidation()
    {
        var act = () => _service.ConfirmPasswordResetAsync(new PasswordResetConfirmDto("", "newpassword123"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WhenTokenInvalid_ThrowsInvalidPasswordResetToken()
    {
        _passwordResetTokens.GetValidByHashAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var act = () => _service.ConfirmPasswordResetAsync(new PasswordResetConfirmDto("rawtoken", "newpassword123"));

        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WhenUserMissing_ThrowsInvalidPasswordResetToken()
    {
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _passwordResetTokens.GetValidByHashAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(resetToken);
        _users.GetByIdAsync(resetToken.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _service.ConfirmPasswordResetAsync(new PasswordResetConfirmDto("rawtoken", "newpassword123"));

        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WhenUserIsGoogle_ThrowsInvalidPasswordResetToken()
    {
        var user = TestData.User(provider: AuthProvider.Google, passwordHash: null);
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _passwordResetTokens.GetValidByHashAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(resetToken);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _service.ConfirmPasswordResetAsync(new PasswordResetConfirmDto("rawtoken", "newpassword123"));

        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    // ---------------------------------------------------------------------
    // RegisterContractorWithGoogleAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RegisterContractorWithGoogleAsync_WithValidToken_PersistsGoogleUser()
    {
        SetupGoogle(email: "boss@example.com", subject: "sub-123");
        _users.EmailExistsAsync("boss@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _contractors.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterContractorGoogleDto("id-token", ValidCnpj, "Empresa Ltda", "Empresa");

        var result = await _service.RegisterContractorWithGoogleAsync(dto);

        result.User.Email.Should().Be("boss@example.com");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.AuthProvider == AuthProvider.Google
                && u.GoogleId == "sub-123"
                && u.PasswordHash == null
                && u.ProfileType == ProfileType.Contratante),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterContractorWithGoogleAsync_WhenGoogleEmailNotVerified_ThrowsInvalidGoogleToken()
    {
        SetupGoogle(emailVerified: false);

        var dto = new RegisterContractorGoogleDto("id-token", ValidCnpj, "Empresa Ltda", null);

        var act = () => _service.RegisterContractorWithGoogleAsync(dto);

        await act.Should().ThrowAsync<InvalidGoogleTokenException>();
    }

    [Fact]
    public async Task RegisterContractorWithGoogleAsync_WithInvalidCompanyData_ThrowsValidation()
    {
        SetupGoogle();

        var dto = new RegisterContractorGoogleDto("id-token", "123", "", null);

        var act = () => _service.RegisterContractorWithGoogleAsync(dto);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field == "cnpj");
        ex.Which.Errors.Should().Contain(e => e.Field == "razaoSocial");
    }

    [Fact]
    public async Task RegisterContractorWithGoogleAsync_WhenEmailExists_ThrowsEmailAlreadyExists()
    {
        SetupGoogle(email: "boss@example.com");
        _users.EmailExistsAsync("boss@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterContractorGoogleDto("id-token", ValidCnpj, "Empresa Ltda", null);

        var act = () => _service.RegisterContractorWithGoogleAsync(dto);

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterContractorWithGoogleAsync_WhenCnpjExists_ThrowsCnpjAlreadyExists()
    {
        SetupGoogle(email: "boss@example.com");
        _users.EmailExistsAsync("boss@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _contractors.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterContractorGoogleDto("id-token", ValidCnpj, "Empresa Ltda", null);

        var act = () => _service.RegisterContractorWithGoogleAsync(dto);

        await act.Should().ThrowAsync<CnpjAlreadyExistsException>();
    }

    // ---------------------------------------------------------------------
    // RegisterCarrierByInviteWithGoogleAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WithNewCarrier_PersistsAll()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        SetupGoogle(email: "carrier@example.com", subject: "sub-c");
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.EmailExistsAsync("carrier@example.com", Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterCarrierGoogleDto("id-token", ValidCnpj, "Transp Ltda", "Transp");

        var result = await _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        result.User.ProfileType.Should().Be("TRANSPORTADOR");
        invite.IsUsed.Should().BeTrue();
        await _carriers.Received(1).AddAsync(Arg.Any<Carrier>(), Arg.Any<CancellationToken>());
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.AuthProvider == AuthProvider.Google && u.GoogleId == "sub-c"),
            Arg.Any<CancellationToken>());
        await _connections.Received(1).AddAsync(Arg.Any<PartnerConnection>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WhenInviteInvalid_ThrowsInvalidInvite()
    {
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns((PartnershipInvite?)null);

        var dto = new RegisterCarrierGoogleDto("id-token", ValidCnpj, "Transp Ltda", null);

        var act = () => _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WhenContractorMissing_ThrowsInvalidInvite()
    {
        var invite = ValidInvite(Guid.NewGuid());
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(invite.ContractorId, Arg.Any<CancellationToken>()).Returns((Contractor?)null);

        var dto = new RegisterCarrierGoogleDto("id-token", ValidCnpj, "Transp Ltda", null);

        var act = () => _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WhenGoogleInvalid_ThrowsInvalidGoogleToken()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        SetupGoogle(emailVerified: false);

        var dto = new RegisterCarrierGoogleDto("id-token", ValidCnpj, "Transp Ltda", null);

        var act = () => _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        await act.Should().ThrowAsync<InvalidGoogleTokenException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WhenCarrierExists_ThrowsCarrierAlreadyRegistered()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        SetupGoogle(email: "carrier@example.com");
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterCarrierGoogleDto("id-token", ValidCnpj, "Transp Ltda", null);

        var act = () => _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        await act.Should().ThrowAsync<CarrierAlreadyRegisteredException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WithInvalidCompanyData_ThrowsValidation()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        SetupGoogle(email: "carrier@example.com");
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterCarrierGoogleDto("id-token", "123", "", null);

        var act = () => _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogleAsync_WhenEmailExists_ThrowsEmailAlreadyExists()
    {
        var contractor = TestData.Contractor();
        var invite = ValidInvite(contractor.Id);
        _invites.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(invite);
        _contractors.GetByIdAsync(contractor.Id, Arg.Any<CancellationToken>()).Returns(contractor);
        SetupGoogle(email: "carrier@example.com");
        _carriers.CnpjExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.EmailExistsAsync("carrier@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterCarrierGoogleDto("id-token", ValidCnpj, "Transp Ltda", null);

        var act = () => _service.RegisterCarrierByInviteWithGoogleAsync("tok", dto);

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    // ---------------------------------------------------------------------
    // LoginWithGoogleAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoginWithGoogleAsync_WhenUserFoundByGoogleId_ReturnsAuthResult()
    {
        SetupGoogle(email: "user@example.com", subject: "sub-x");
        var user = TestData.User(email: "user@example.com", provider: AuthProvider.Google, passwordHash: null);
        _users.GetByGoogleIdAsync("sub-x", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _service.LoginWithGoogleAsync(new GoogleLoginDto("id-token"));

        result.Token.Should().Be("jwt-token");
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WhenUserFoundByEmailFallback_ReturnsAuthResult()
    {
        SetupGoogle(email: "user@example.com", subject: "sub-x");
        _users.GetByGoogleIdAsync("sub-x", Arg.Any<CancellationToken>()).Returns((User?)null);
        var user = TestData.User(email: "user@example.com", provider: AuthProvider.Google, passwordHash: null);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _service.LoginWithGoogleAsync(new GoogleLoginDto("id-token"));

        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WhenGoogleInvalid_ThrowsInvalidGoogleToken()
    {
        SetupGoogle(emailVerified: false);

        var act = () => _service.LoginWithGoogleAsync(new GoogleLoginDto("id-token"));

        await act.Should().ThrowAsync<InvalidGoogleTokenException>();
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WhenUserNotFound_ThrowsInvalidCredentials()
    {
        SetupGoogle(email: "user@example.com", subject: "sub-x");
        _users.GetByGoogleIdAsync("sub-x", Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _service.LoginWithGoogleAsync(new GoogleLoginDto("id-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WhenUserIsLocal_ThrowsInvalidCredentials()
    {
        SetupGoogle(email: "user@example.com", subject: "sub-x");
        var user = TestData.User(email: "user@example.com", provider: AuthProvider.Local, passwordHash: "hash");
        _users.GetByGoogleIdAsync("sub-x", Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _service.LoginWithGoogleAsync(new GoogleLoginDto("id-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ---------------------------------------------------------------------
    // RegisterAdministratorWithGoogleAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RegisterAdministratorWithGoogleAsync_WithValidToken_PersistsAdminUser()
    {
        SetupGoogle(email: "admin@example.com", subject: "sub-admin");
        _users.EmailExistsAsync("admin@example.com", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.RegisterAdministratorWithGoogleAsync(new RegisterAdministratorGoogleDto("id-token"));

        result.User.ProfileType.Should().Be("ADMIN");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.ProfileType == ProfileType.Admin
                && u.AuthProvider == AuthProvider.Google
                && u.GoogleId == "sub-admin"
                && u.CompanyId == null),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAdministratorWithGoogleAsync_WhenGoogleInvalid_ThrowsInvalidGoogleToken()
    {
        SetupGoogle(emailVerified: false);

        var act = () => _service.RegisterAdministratorWithGoogleAsync(new RegisterAdministratorGoogleDto("id-token"));

        await act.Should().ThrowAsync<InvalidGoogleTokenException>();
    }

    [Fact]
    public async Task RegisterAdministratorWithGoogleAsync_WhenEmailExists_ThrowsEmailAlreadyExists()
    {
        SetupGoogle(email: "admin@example.com");
        _users.EmailExistsAsync("admin@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _service.RegisterAdministratorWithGoogleAsync(new RegisterAdministratorGoogleDto("id-token"));

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }
}
