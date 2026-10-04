using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Application.Exceptions;
using Sigloc.Infrastructure.Authentication;
using Sigloc.Infrastructure.Configurations;
using Sigloc.Infrastructure.Notifications;

namespace Sigloc.Tests.Unit;

public class AuthInfrastructureTests
{
    // ---------------------------------------------------------------------
    // JwtProvider
    // ---------------------------------------------------------------------

    private static JwtProvider CreateJwtProvider(out JwtSettings settings)
    {
        settings = new JwtSettings
        {
            SecretKey = "super-secret-key-that-is-long-enough-32+",
            Issuer = "sigloc-issuer",
            Audience = "sigloc-audience",
            ExpiryMinutes = 60
        };
        return new JwtProvider(Options.Create(settings));
    }

    [Fact]
    public void Generate_produces_parseable_jwt_with_expected_metadata_and_claims_with_company()
    {
        var provider = CreateJwtProvider(out var settings);
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var jwt = provider.Generate(userId, "user@example.com", "Admin", "gestor", companyId);

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(jwt).Should().BeTrue();
        var token = handler.ReadJwtToken(jwt);

        token.Issuer.Should().Be(settings.Issuer);
        token.Audiences.Should().Contain(settings.Audience);
        token.ValidTo.Should().BeCloseTo(before.AddMinutes(settings.ExpiryMinutes), TimeSpan.FromMinutes(1));

        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@example.com");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        token.Claims.Should().Contain(c => c.Type == "tipoPerfil" && c.Value == "gestor");
        token.Claims.Should().Contain(c => c.Type == "empresaId" && c.Value == companyId.ToString());
    }

    [Fact]
    public void Generate_omits_empresaId_claim_when_company_is_null()
    {
        var provider = CreateJwtProvider(out _);

        var jwt = provider.Generate(Guid.NewGuid(), "user@example.com", "Driver", "motorista", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        token.Claims.Should().NotContain(c => c.Type == "empresaId");
        token.Claims.Should().Contain(c => c.Type == "tipoPerfil" && c.Value == "motorista");
    }

    // ---------------------------------------------------------------------
    // BCryptPasswordHasher
    // ---------------------------------------------------------------------

    [Fact]
    public void Hash_then_Verify_returns_true_for_correct_password()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("P@ssw0rd!");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("P@ssw0rd!");
        hasher.Verify("P@ssw0rd!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("correct-horse");

        hasher.Verify("battery-staple", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_returns_false_for_malformed_hash_instead_of_throwing()
    {
        var hasher = new BCryptPasswordHasher();

        var act = () => hasher.Verify("anything", "not-a-bcrypt-hash");

        act.Should().NotThrow();
        act().Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // InviteLinkBuilder
    // ---------------------------------------------------------------------

    [Fact]
    public void InviteLinkBuilder_appends_token_and_trims_trailing_slash()
    {
        var builder = new InviteLinkBuilder(Options.Create(new InviteSettings
        {
            FrontendBaseUrl = "https://app.sigloc.com/convite/"
        }));

        builder.Build("abc123").Should().Be("https://app.sigloc.com/convite/abc123");
    }

    [Fact]
    public void InviteLinkBuilder_without_trailing_slash_still_joins_with_single_slash()
    {
        var builder = new InviteLinkBuilder(Options.Create(new InviteSettings
        {
            FrontendBaseUrl = "https://app.sigloc.com/convite"
        }));

        builder.Build("abc123").Should().Be("https://app.sigloc.com/convite/abc123");
    }

    [Fact]
    public void InviteLinkBuilder_with_empty_base_url_returns_token_alone()
    {
        var builder = new InviteLinkBuilder(Options.Create(new InviteSettings
        {
            FrontendBaseUrl = string.Empty
        }));

        builder.Build("tok-only").Should().Be("tok-only");
    }

    // ---------------------------------------------------------------------
    // PasswordResetLinkBuilder
    // ---------------------------------------------------------------------

    [Fact]
    public void PasswordResetLinkBuilder_uses_question_mark_when_no_existing_query()
    {
        var builder = new PasswordResetLinkBuilder(Options.Create(new PasswordResetSettings
        {
            FrontendBaseUrl = "https://app.sigloc.com/reset",
            TokenExpiryMinutes = 45
        }));

        builder.Build("tok 1").Should().Be("https://app.sigloc.com/reset?token=tok%201");
    }

    [Fact]
    public void PasswordResetLinkBuilder_uses_ampersand_when_query_already_present()
    {
        var builder = new PasswordResetLinkBuilder(Options.Create(new PasswordResetSettings
        {
            FrontendBaseUrl = "https://app.sigloc.com/reset?lang=pt",
            TokenExpiryMinutes = 30
        }));

        builder.Build("a/b+c").Should().Be("https://app.sigloc.com/reset?lang=pt&token=a%2Fb%2Bc");
    }

    [Fact]
    public void PasswordResetLinkBuilder_exposes_token_expiry_from_settings()
    {
        var builder = new PasswordResetLinkBuilder(Options.Create(new PasswordResetSettings
        {
            FrontendBaseUrl = "https://app.sigloc.com/reset",
            TokenExpiryMinutes = 123
        }));

        builder.TokenExpiryMinutes.Should().Be(123);
    }

    // ---------------------------------------------------------------------
    // SmtpEmailSender
    // ---------------------------------------------------------------------

    [Fact]
    public async Task SmtpEmailSender_throws_when_host_is_missing()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpSettings
        {
            Host = string.Empty,
            FromAddress = "noreply@sigloc.com"
        }));

        var act = async () => await sender.SendPasswordResetAsync("user@example.com", "https://link");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SMTP is not configured.");
    }

    [Fact]
    public async Task SmtpEmailSender_throws_when_from_address_is_missing()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpSettings
        {
            Host = "smtp.sigloc.com",
            FromAddress = string.Empty
        }));

        var act = async () => await sender.SendPasswordResetAsync("user@example.com", "https://link");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SMTP is not configured.");
    }

    // ---------------------------------------------------------------------
    // GoogleTokenVerifier
    // ---------------------------------------------------------------------

    [Fact]
    public void GoogleTokenVerifier_can_be_constructed_from_settings()
    {
        var verifier = new GoogleTokenVerifier(Options.Create(new GoogleAuthSettings
        {
            ClientId = "client-id.apps.googleusercontent.com"
        }));

        verifier.Should().BeAssignableTo<IGoogleTokenVerifier>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GoogleTokenVerifier_throws_for_blank_token(string idToken)
    {
        var verifier = new GoogleTokenVerifier(Options.Create(new GoogleAuthSettings
        {
            ClientId = "client-id"
        }));

        var act = async () => await verifier.VerifyAsync(idToken);

        await act.Should().ThrowAsync<InvalidGoogleTokenException>();
    }

    [Fact]
    public async Task GoogleTokenVerifier_throws_for_malformed_token_without_network()
    {
        // A structurally invalid JWT fails local parsing in the Google library
        // (InvalidJwtException) before any network call to Google's key endpoint.
        var verifier = new GoogleTokenVerifier(Options.Create(new GoogleAuthSettings
        {
            ClientId = "client-id"
        }));

        var act = async () => await verifier.VerifyAsync("not-a-valid-google-jwt");

        await act.Should().ThrowAsync<InvalidGoogleTokenException>();
    }
}
