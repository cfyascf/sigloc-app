using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sigloc.Api.Controllers;
using Sigloc.Api.Middleware;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Constants;

namespace Sigloc.Tests;

public class TripHttpTests
{
    [Theory]
    [InlineData(null, 401)] [InlineData("Carrier", 403)] [InlineData("Shipper", 200)]
    public async Task Active_endpoint_enforces_authentication_and_shipper_policy(string? role, int status)
    {
        var h = new TripMonitoringServiceTests.Harness();
        using var server = Server(h);
        using var client = Client(server, h.CompanyId, role);
        var response = await client.GetAsync("/api/viagens/ativas");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(0, h.Tracking.Calls); Assert.Equal(0, h.Store.RefreshCalls);
        if (status == 200)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.TryGetProperty("viagens", out _));
            Assert.Equal(20, json.RootElement.GetProperty("pageSize").GetInt32());
        }
    }

    [Fact]
    public async Task Detail_without_refresh_never_contacts_tracking_provider()
    {
        // A plain page load must return the stored snapshot without consuming the GPS
        // tracking/routing provider quota; only the explicit refresh does that.
        var h = new TripMonitoringServiceTests.Harness();
        using var server = Server(h); using var client = Client(server, h.CompanyId, Roles.Shipper);

        var response = await client.GetAsync($"/api/viagens/{h.State.Trip.Id}/detalhes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, h.Tracking.Calls);
        Assert.Equal(0, h.Store.RefreshCalls);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("isCacheRenovado").GetBoolean());
    }

    [Fact]
    public async Task Detail_returns_no_store_and_reuses_successful_cache()
    {
        var h = new TripMonitoringServiceTests.Harness();
        using var server = Server(h); using var client = Client(server, h.CompanyId, Roles.Shipper);
        var first = await client.GetAsync($"/api/viagens/{h.State.Trip.Id}/detalhes?refresh=true");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True(first.Headers.CacheControl!.NoStore);
        var second = await client.GetAsync($"/api/viagens/{h.State.Trip.Id}/detalhes?refresh=true");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var json = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("isCacheRenovado").GetBoolean());
        Assert.Equal(h.State.Trip.Id, json.RootElement.GetProperty("viagemId").GetGuid());
        Assert.Equal(h.State.Route.Id, json.RootElement.GetProperty("rotaId").GetGuid());
        Assert.True(json.RootElement.TryGetProperty("saudeOperacao", out _));
        Assert.True(json.RootElement.TryGetProperty("linhaDoTempo", out _));
        Assert.Equal(1, h.Tracking.Calls); Assert.Equal(1, h.Store.RefreshCalls);
    }

    [Fact]
    public async Task Cross_tenant_is_404_and_unavailable_without_cache_is_standard_503()
    {
        var h = new TripMonitoringServiceTests.Harness();
        using var server = Server(h);
        using (var other = Client(server, Guid.NewGuid(), Roles.Shipper))
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/viagens/{h.State.Trip.Id}/detalhes")).StatusCode);
        h.State.Vehicle.TraccarDeviceId = null;
        using var client = Client(server, h.CompanyId, Roles.Shipper);
        var response = await client.GetAsync($"/api/viagens/{h.State.Trip.Id}/detalhes?refresh=true");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("TRACKING_UNAVAILABLE", json.RootElement.GetProperty("error").GetString());
        Assert.False(json.RootElement.TryGetProperty("stackTrace", out _));
    }

    [Theory]
    [InlineData("status=invalid")] [InlineData("risk=invalid")] [InlineData("page=2147483647")]
    public async Task Invalid_queries_are_400(string query)
    {
        var h = new TripMonitoringServiceTests.Harness();
        using var server = Server(h); using var client = Client(server, h.CompanyId, Roles.Shipper);
        var response = await client.GetAsync("/api/viagens/ativas?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static TestServer Server(TripMonitoringServiceTests.Harness h) => new(new WebHostBuilder()
        .ConfigureServices(services =>
        {
            services.AddLogging(); services.AddRouting();
            services.AddSingleton<ITripMonitoringService>(h.Service);
            services.AddControllers().AddApplicationPart(typeof(TripsController).Assembly);
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
            services.AddAuthorization(options => options.AddPolicy(Policies.RequireShipperAccess, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Shipper, Roles.Admin)));
        })
        .Configure(app =>
        {
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));
    private static HttpClient Client(TestServer server, Guid companyId, string? role)
    {
        var client = server.CreateClient();
        if (role != null) { client.DefaultRequestHeaders.Add("Test-Role", role); client.DefaultRequestHeaders.Add("Test-Company", companyId.ToString()); }
        return client;
    }
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Test-Role", out var role)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role.ToString()), new Claim("empresaId", Request.Headers["Test-Company"].ToString()) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
