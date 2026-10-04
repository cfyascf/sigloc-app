using System.Net;
using System.Net.Http.Json;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests.Unit.Controllers;

public class AuthControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static AuthResultDto AuthResult() =>
        new("jwt-token", new AuthUserDto(Guid.NewGuid(), "user@example.com", "Contratante", CompanyId));

    // ---- Anonymous endpoints -------------------------------------------------

    [Fact]
    public async Task RegisterContractor_is_anonymous_and_returns_created()
    {
        var service = Substitute.For<IAuthService>();
        service.RegisterContractorAsync(Arg.Any<RegisterContractorDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/register/contratante",
            new RegisterContractorDto("11222333000181", "Co", "Trade", "e@e.com", "pw"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await service.Received(1).RegisterContractorAsync(Arg.Any<RegisterContractorDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_is_anonymous_and_returns_ok()
    {
        var service = Substitute.For<IAuthService>();
        service.LoginAsync(Arg.Any<LoginDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto("e@e.com", "pw"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).LoginAsync(Arg.Any<LoginDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginWithGoogle_is_anonymous_and_returns_ok()
    {
        var service = Substitute.For<IAuthService>();
        service.LoginWithGoogleAsync(Arg.Any<GoogleLoginDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/login/google", new GoogleLoginDto("id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).LoginWithGoogleAsync(Arg.Any<GoogleLoginDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterContractorWithGoogle_is_anonymous_and_returns_created()
    {
        var service = Substitute.For<IAuthService>();
        service.RegisterContractorWithGoogleAsync(Arg.Any<RegisterContractorGoogleDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/register/contratante/google",
            new RegisterContractorGoogleDto("id-token", "11222333000181", "Co", "Trade"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await service.Received(1).RegisterContractorWithGoogleAsync(Arg.Any<RegisterContractorGoogleDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateInvite_is_anonymous_and_returns_ok()
    {
        var service = Substitute.For<IAuthService>();
        service.ValidateInviteAsync("tok", Arg.Any<CancellationToken>())
            .Returns(new InviteValidationDto(true, "Co", "ok"));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.GetAsync("/api/auth/invite/tok");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).ValidateInviteAsync("tok", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCarrierByInvite_is_anonymous_and_returns_created()
    {
        var service = Substitute.For<IAuthService>();
        service.RegisterCarrierByInviteAsync("tok", Arg.Any<RegisterCarrierDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/invite/tok/register",
            new RegisterCarrierDto("99888777000166", "Co", "Trade", "e@e.com", "pw"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await service.Received(1).RegisterCarrierByInviteAsync("tok", Arg.Any<RegisterCarrierDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCarrierByInviteWithGoogle_is_anonymous_and_returns_created()
    {
        var service = Substitute.For<IAuthService>();
        service.RegisterCarrierByInviteWithGoogleAsync("tok", Arg.Any<RegisterCarrierGoogleDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/invite/tok/register/google",
            new RegisterCarrierGoogleDto("id-token", "99888777000166", "Co", "Trade"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await service.Received(1).RegisterCarrierByInviteWithGoogleAsync("tok", Arg.Any<RegisterCarrierGoogleDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestPasswordReset_is_anonymous_and_returns_ok()
    {
        var service = Substitute.For<IAuthService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/password-reset/request", new PasswordResetRequestDto("e@e.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).RequestPasswordResetAsync(Arg.Any<PasswordResetRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmPasswordReset_is_anonymous_and_returns_no_content()
    {
        var service = Substitute.For<IAuthService>();
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role: null);

        var response = await client.PostAsJsonAsync("/api/auth/password-reset/confirm", new PasswordResetConfirmDto("tok", "pw"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await service.Received(1).ConfirmPasswordResetAsync(Arg.Any<PasswordResetConfirmDto>(), Arg.Any<CancellationToken>());
    }

    // ---- Admin-only ----------------------------------------------------------

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Admin, 201)]
    public async Task RegisterAdministrator_enforces_admin_policy(string? role, int status)
    {
        var service = Substitute.For<IAuthService>();
        service.RegisterAdministratorWithGoogleAsync(Arg.Any<RegisterAdministratorGoogleDto>(), Arg.Any<CancellationToken>()).Returns(AuthResult());
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/auth/register/administrator", new RegisterAdministratorGoogleDto("id-token"));

        ((int)response.StatusCode).Should().Be(status);
    }

    // ---- Shipper-only invite endpoints --------------------------------------

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Carrier, 403)]
    [InlineData(Roles.Shipper, 201)]
    public async Task CreateInvite_enforces_shipper_policy_and_passes_identity(string? role, int status)
    {
        var service = Substitute.For<IAuthService>();
        service.CreateInviteAsync(CompanyId, Arg.Any<CreateInviteDto>(), Arg.Any<CancellationToken>())
            .Returns(new InviteCreatedDto("tok", "link", DateTimeOffset.UtcNow.AddDays(7)));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsJsonAsync("/api/auth/invite", new CreateInviteDto("e@e.com", 7));

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).CreateInviteAsync(CompanyId, Arg.Any<CreateInviteDto>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetActiveInvite_returns_ok_when_present()
    {
        var service = Substitute.For<IAuthService>();
        service.GetActiveInviteAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new ActiveInviteDto(Guid.NewGuid(), "code", "link", DateTimeOffset.UtcNow.AddDays(1), 24));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/auth/invite/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await service.Received(1).GetActiveInviteAsync(CompanyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetActiveInvite_returns_no_content_when_absent()
    {
        var service = Substitute.For<IAuthService>();
        service.GetActiveInviteAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((ActiveInviteDto?)null);
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, Roles.Shipper);

        var response = await client.GetAsync("/api/auth/invite/active");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---- Carrier-only --------------------------------------------------------

    [Theory]
    [InlineData(null, 401)]
    [InlineData(Roles.Shipper, 403)]
    [InlineData(Roles.Carrier, 201)]
    public async Task ConnectByInvite_enforces_carrier_policy_and_passes_identity(string? role, int status)
    {
        var service = Substitute.For<IAuthService>();
        service.ConnectCarrierByInviteAsync(CompanyId, "tok", Arg.Any<CancellationToken>())
            .Returns(new PartnerConnectionCreatedDto(Guid.NewGuid(), "Contratante", "Active"));
        using var server = ControllerTestHost.Server(service);
        using var client = ControllerTestHost.Client(server, CompanyId, role);

        var response = await client.PostAsync("/api/auth/invite/tok/connect", content: null);

        ((int)response.StatusCode).Should().Be(status);
        if (status == 201)
        {
            await service.Received(1).ConnectCarrierByInviteAsync(CompanyId, "tok", Arg.Any<CancellationToken>());
        }
    }
}
