using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
using static Sigloc.Tests.CarrierTripServiceTests;

namespace Sigloc.Tests;

public class CarrierTripHttpTests
{
    private static readonly byte[] Pdf = [.. "%PDF-1.7\n"u8.ToArray(), 1, 2, 3];

    [Theory]
    [InlineData(null, 401)] [InlineData("Shipper", 403)] [InlineData("Carrier", 200)]
    public async Task Carrier_endpoints_enforce_carrier_policy(string? role, int status)
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, role);

        Assert.Equal(status, (int)(await client.GetAsync("/api/transportador/viagens/ativas")).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync($"/api/transportador/viagens/{c.H.State.Trip.Id}/detalhes")).StatusCode);
    }

    [Fact]
    public async Task Detail_wraps_monitoring_and_never_pings_without_refresh()
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, Roles.Carrier);

        var response = await client.GetAsync($"/api/transportador/viagens/{c.H.State.Trip.Id}/detalhes");

        Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(c.H.State.Trip.Id, json.RootElement.GetProperty("viagem").GetProperty("viagemId").GetGuid());
        Assert.Equal(c.Stops[0].Id, json.RootElement.GetProperty("acaoAtual").GetProperty("paradaId").GetGuid());
        Assert.False(json.RootElement.GetProperty("acaoAtual").GetProperty("exigeComprovante").GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("especificacaoCarga", out _));
        Assert.Equal(0, c.H.Tracking.Calls);
    }

    [Fact]
    public async Task Other_carrier_gets_404()
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, Roles.Carrier, companyId: Guid.NewGuid());
        var response = await client.GetAsync($"/api/transportador/viagens/{c.H.State.Trip.Id}/detalhes");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Multipart_completion_confirms_pickup_then_delivery_with_proof()
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, Roles.Carrier);

        var pickup = await client.PostAsync(StopUrl(c, c.Stops[0].Id), Form(note: "Coletado na doca 3"));
        Assert.Equal(HttpStatusCode.OK, pickup.StatusCode);

        var missing = await client.PostAsync(StopUrl(c, c.Stops[1].Id), EmptyBrowserForm());
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("comprovante", (await Error(missing)).GetProperty("details")[0].GetProperty("field").GetString());

        var delivery = await client.PostAsync(StopUrl(c, c.Stops[1].Id), Form(Pdf, "canhoto.pdf"));
        Assert.Equal(HttpStatusCode.OK, delivery.StatusCode);
        using var json = JsonDocument.Parse(await delivery.Content.ReadAsStringAsync());
        Assert.Equal("ENTREGUE", json.RootElement.GetProperty("viagem").GetProperty("statusGeral").GetString());
        var proofId = json.RootElement.GetProperty("comprovantes")[0].GetProperty("id").GetGuid();

        var download = await client.GetAsync($"/api/transportador/viagens/{c.H.State.Trip.Id}/comprovantes/{proofId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Pdf, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Disguised_file_is_400_and_non_actionable_stop_is_409()
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, Roles.Carrier);

        var disguised = await client.PostAsync(StopUrl(c, c.Stops[0].Id), Form([0x4D, 0x5A, 0x90, 0x00], "canhoto.pdf"));
        Assert.Equal(HttpStatusCode.BadRequest, disguised.StatusCode);

        var skipped = await client.PostAsync(StopUrl(c, c.Stops[1].Id), Form(Pdf, "canhoto.pdf"));
        Assert.Equal(HttpStatusCode.Conflict, skipped.StatusCode);
        Assert.Equal("TRIP_STOP_NOT_ACTIONABLE", (await Error(skipped)).GetProperty("error").GetString());
        Assert.Empty(c.Files.Stored);
    }

    [Fact]
    public async Task Occurrence_is_created_with_location_and_validated()
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, Roles.Carrier);
        var url = $"/api/transportador/viagens/{c.H.State.Trip.Id}/ocorrencias";

        var created = await client.PostAsJsonAsync(url, new { tipo = "TRANSITO_INTENSO", observacao = "Lentidão na Régis" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.EndsWith($"/api/transportador/viagens/{c.H.State.Trip.Id}/detalhes", created.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal("WARNING", json.RootElement.GetProperty("viagem").GetProperty("indicadorRisco").GetString());
        Assert.Equal("ATENCAO", json.RootElement.GetProperty("viagem").GetProperty("saudeOperacao").GetProperty("statusSla").GetString());
        Assert.Equal("Trânsito Intenso", json.RootElement.GetProperty("ocorrencias")[0].GetProperty("rotulo").GetString());

        var invalid = await client.PostAsJsonAsync(url, new { tipo = "OUTRO" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Mutations_require_a_user_id_claim()
    {
        var c = new CarrierHarness();
        using var server = Server(c); using var client = Client(server, c, Roles.Carrier, includeUser: false);
        var response = await client.PostAsJsonAsync($"/api/transportador/viagens/{c.H.State.Trip.Id}/ocorrencias", new { tipo = "RISCO_SLA" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(c.H.Store.State!.Occurrences);
    }

    private static string StopUrl(CarrierHarness c, Guid stopId) =>
        $"/api/transportador/viagens/{c.H.State.Trip.Id}/paradas/{stopId}/conclusao";

    private static MultipartFormDataContent Form(byte[]? file = null, string? name = null, string? note = null)
    {
        var form = new MultipartFormDataContent();
        if (file != null)
        {
            var content = new ByteArrayContent(file);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(content, "comprovante", name ?? "canhoto.pdf");
        }
        if (note != null) form.Add(new StringContent(note), "observacao");
        return form;
    }

    /// <summary>What a browser sends for an empty FormData (HttpClient's empty multipart is malformed).</summary>
    private static ByteArrayContent EmptyBrowserForm()
    {
        var content = new ByteArrayContent("------WebKitFormBoundaryX--\r\n"u8.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=----WebKitFormBoundaryX");
        return content;
    }

    private static async Task<JsonElement> Error(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static TestServer Server(CarrierHarness c) => new(new WebHostBuilder()
        .ConfigureServices(services =>
        {
            services.AddLogging(); services.AddRouting();
            services.AddSingleton<ICarrierTripService>(c.Service);
            services.AddControllers().AddApplicationPart(typeof(CarrierTripsController).Assembly);
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.RequireShipperAccess, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Shipper, Roles.Admin));
                options.AddPolicy(Policies.RequireCarrierAccess, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Carrier, Roles.Admin));
            });
        })
        .Configure(app =>
        {
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));

    private static HttpClient Client(TestServer server, CarrierHarness c, string? role, Guid? companyId = null, bool includeUser = true)
    {
        var client = server.CreateClient();
        if (role == null) return client;
        client.DefaultRequestHeaders.Add("Test-Role", role);
        client.DefaultRequestHeaders.Add("Test-Company", (companyId ?? c.CarrierId).ToString());
        if (includeUser) client.DefaultRequestHeaders.Add("Test-User", c.UserId.ToString());
        return client;
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Test-Role", out var role)) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.Role, role.ToString()), new("empresaId", Request.Headers["Test-Company"].ToString()) };
            if (Request.Headers.TryGetValue("Test-User", out var user)) claims.Add(new Claim(ClaimTypes.NameIdentifier, user.ToString()));
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
