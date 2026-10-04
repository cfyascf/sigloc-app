using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sigloc.Api.Middleware;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;
using Sigloc.Domain.Enums;

namespace Sigloc.Tests.Unit.Controllers;

/// <summary>
/// Shared in-process TestServer helpers for controller HTTP tests. Mirrors the proven
/// pattern in <c>TripHttpTests</c>: a TestServer with a single mocked service, a
/// <see cref="TestAuthentication"/> scheme that reads Test-Role/Test-Company headers and
/// the real authorization policies the controllers depend on.
/// </summary>
internal static class ControllerTestHost
{
    /// <summary>
    /// Builds a TestServer that registers the given service instance (as <typeparamref name="TService"/>),
    /// maps the controller assembly, wires test authentication and all app policies.
    /// </summary>
    public static TestServer Server<TService>(TService service) where TService : class =>
        new(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
                services.AddSingleton(service);
                services.AddControllers().AddApplicationPart(typeof(Sigloc.Api.Controllers.TripsController).Assembly);
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
                services.AddAuthorization(options =>
                {
                    options.AddPolicy(Policies.RequireShipperAccess, p =>
                        p.RequireAuthenticatedUser().RequireRole(Roles.Shipper, Roles.Admin));
                    options.AddPolicy(Policies.RequireCarrierAccess, p =>
                        p.RequireAuthenticatedUser().RequireRole(Roles.Carrier, Roles.Admin));
                    options.AddPolicy(Policies.RequireAdminAccess, p =>
                        p.RequireAuthenticatedUser().RequireRole(Roles.Admin));
                });
            })
            .Configure(app =>
            {
                app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));

    /// <summary>
    /// Creates a client for the given identity. When <paramref name="role"/> is null the
    /// request is unauthenticated (expect 401 on protected routes).
    /// </summary>
    public static HttpClient Client(TestServer server, Guid companyId, string? role)
    {
        var client = server.CreateClient();
        if (role != null)
        {
            client.DefaultRequestHeaders.Add("Test-Role", role);
            client.DefaultRequestHeaders.Add("Test-Company", companyId.ToString());
        }

        return client;
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Test-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Role, role.ToString()),
                    new Claim("empresaId", Request.Headers["Test-Company"].ToString()),
                },
                Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}

/// <summary>Minimal, valid response DTO factories for the <c>CreatedAtAction</c> paths.</summary>
internal static class ControllerDtoFactory
{
    /// <summary>
    /// A non-null instance of <typeparamref name="T"/> for <c>Ok(result)</c> happy paths.
    /// ASP.NET Core turns <c>Ok(null)</c> into 204; a non-null value keeps the expected 200.
    /// The instance is created without running constructors, so nested DTO graphs don't
    /// need to be hand-built just to exercise the controller→service→200 wiring.
    /// </summary>
    public static T Stub<T>() where T : class =>
        (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));

    public static VehicleResponseDto Vehicle(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        TransportadoraId: Guid.NewGuid(),
        Plate: "ABC1D23",
        Model: "Volvo FH",
        AxleCount: 3,
        CapacityWeight: 20000m,
        CapacityVolume: 80m,
        BodyType: VehicleBodyType.Bau,
        RefrigerationLevel: RefrigerationLevel.Nenhuma,
        HasMopp: false,
        HasCargoSecuring: true,
        Driver: "João",
        CurrentLocation: "Curitiba, PR",
        Status: OperationalStatus.LIVRE);

    public static ProductResponseDto Product(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        ContractorId: Guid.NewGuid(),
        Sku: "SKU-001",
        Name: "Caixa Padrão",
        Type: null,
        Category: "General",
        TransportEnvironment: "Dry",
        TempMin: null,
        TempMax: null,
        PackagingType: null,
        Dangerous: false,
        Fragile: false,
        DefaultWeight: 10,
        DefaultVolume: 1,
        HandlingRestriction: null,
        VehicleRequirement: new VehicleRequirementDto("Bau", "Nenhuma", false, false),
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow);

    public static RouteSegmentResponseDto RouteSegment(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        ContractorId: Guid.NewGuid(),
        RouteId: null,
        Origin: "Curitiba, PR",
        Destination: "São Paulo, SP",
        DistanceKm: 408,
        EstimatedTimeHours: 6,
        BudgetCeiling: 5000m,
        EstimatedTollCost: 120m,
        PickupDeadline: DateTimeOffset.UtcNow.AddDays(1),
        DeliveryDeadline: DateTimeOffset.UtcNow.AddDays(2),
        Status: "Draft",
        Items: Array.Empty<RouteSegmentItemDto>(),
        CalculatedTotals: new CalculatedTotalsDto(0, 0),
        ConsolidatedVehicleRequirement: new ConsolidatedVehicleRequirementDto("Bau", "Nenhuma", false, false),
        CreatedAt: DateTimeOffset.UtcNow);

    public static CreateAuctionResponseDto Auction(Guid? id = null) => new(
        Auction: new AuctionDto(
            Id: id ?? Guid.NewGuid(),
            RouteId: Guid.NewGuid(),
            OpenedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(1),
            AutomaticAward: false,
            Status: "Open"),
        ConsolidatedRoute: new ConsolidatedRouteDto(
            Id: Guid.NewGuid(),
            Status: "Consolidated",
            TotalDistanceKm: 408,
            EstimatedTimeHours: 6,
            TotalWeightKg: 1000,
            TotalVolumeM3: 10,
            ConsolidatedCeiling: 5000m,
            EstimatedAnttFloor: 3000m),
        UpdatedSegments: 1);
}
