using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sigloc.Api.Extensions;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Authentication;

namespace Sigloc.Tests.Unit;

public class TokenVersionValidatorTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private TokenValidatedContext Context(string? version)
    {
        var services = new ServiceCollection().AddSingleton(_users).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _userId.ToString()) };
        if (version != null) claims.Add(new Claim(JwtProvider.TokenVersionClaim, version));
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        return new TokenValidatedContext(http, scheme, new JwtBearerOptions())
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
        };
    }

    [Fact]
    public async Task A_token_with_the_current_version_is_accepted()
    {
        _users.GetTokenVersionAsync(_userId, Arg.Any<CancellationToken>()).Returns(2);
        var context = Context("2");

        await TokenVersionValidator.ValidateAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task A_token_issued_before_logout_is_rejected()
    {
        _users.GetTokenVersionAsync(_userId, Arg.Any<CancellationToken>()).Returns(3);
        var context = Context("2");

        await TokenVersionValidator.ValidateAsync(context);

        context.Result!.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task A_token_from_before_versioning_counts_as_version_zero()
    {
        _users.GetTokenVersionAsync(_userId, Arg.Any<CancellationToken>()).Returns(0);
        var context = Context(null);

        await TokenVersionValidator.ValidateAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task A_token_of_a_deleted_user_is_rejected()
    {
        _users.GetTokenVersionAsync(_userId, Arg.Any<CancellationToken>()).Returns((int?)null);
        var context = Context("0");

        await TokenVersionValidator.ValidateAsync(context);

        context.Result!.Failure.Should().NotBeNull();
    }
}
