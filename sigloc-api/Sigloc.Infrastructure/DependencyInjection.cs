using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Repositories;
using System.Net.Http.Headers;
using Sigloc.Infrastructure.Authentication;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Configurations;
using Sigloc.Infrastructure.Notifications;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Infrastructure.Routing;
using Sigloc.Infrastructure.Monitoring;

namespace Sigloc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        void ConfigureDbContext(DbContextOptionsBuilder options) =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null));

        // Register the scoped DbContext with singleton-scoped options so it can coexist
        // with the DbContext factory below (EF requires the options lifetime to be
        // singleton when both are registered for the same context type).
        services.AddDbContext<SiglocDbContext>(ConfigureDbContext, optionsLifetime: ServiceLifetime.Singleton);

        // Factory used by services that fan out independent queries in parallel
        // (e.g. the dashboard aggregator), so each concurrent query gets its own
        // short-lived DbContext instead of sharing the scoped one (which is not thread-safe).
        services.AddDbContextFactory<SiglocDbContext>(ConfigureDbContext, lifetime: ServiceLifetime.Singleton);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.Configure<InviteSettings>(configuration.GetSection(InviteSettings.SectionName));
        services.Configure<PasswordResetSettings>(configuration.GetSection(PasswordResetSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));

        var openRouteSettings = configuration
            .GetSection(OpenRouteServiceSettings.SectionName)
            .Get<OpenRouteServiceSettings>() ?? new OpenRouteServiceSettings();

        services.AddHttpClient<IRouteGeocodingService, OpenRouteServiceGeocodingService>(client =>
        {
            client.BaseAddress = new Uri(openRouteSettings.BaseUrl);
            if (!string.IsNullOrWhiteSpace(openRouteSettings.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", openRouteSettings.ApiKey);
            }

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddSingleton<IInviteLinkBuilder, InviteLinkBuilder>();
        services.AddSingleton<IPasswordResetLinkBuilder, PasswordResetLinkBuilder>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRouteSegmentRepository, RouteSegmentRepository>();
        services.AddScoped<IConsolidatedRouteRepository, ConsolidatedRouteRepository>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<ITripMonitoringStore, TripMonitoringStore>();
        services.AddSingleton(TimeProvider.System);
        services.AddOptions<TripMonitoringOptions>().Bind(configuration.GetSection("Monitoring"))
            .Validate(o => o.MaxGpsAgeMinutes > 0 && double.IsFinite(o.GeofenceRadiusMeters)
                && o.GeofenceRadiusMeters > 0 && o.DockBufferMinutes >= 0, "Invalid monitoring settings.");
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripMonitoringOptions>>().Value);
        services.Configure<MonitoringProviderSettings>(configuration.GetSection("Monitoring"));
        services.Configure<OpenRouteServiceSettings>(configuration.GetSection(OpenRouteServiceSettings.SectionName));
        if (configuration.GetValue<bool>("Monitoring:MockMode"))
        {
            services.AddScoped<ITripTrackingProvider, MockTripTrackingProvider>();
            services.AddScoped<ITripRoutingProvider, MockTripRoutingProvider>();
        }
        else
        {
            services.AddHttpClient<ITripTrackingProvider, TraccarTripTrackingProvider>();
            services.AddHttpClient<ITripRoutingProvider, OpenRouteServiceTripRoutingProvider>();
        }
        services.AddScoped<IBlockedBidAttemptRepository, BlockedBidAttemptRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAuctionNotifier, LoggingAuctionNotifier>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IContractorRepository, ContractorRepository>();
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddScoped<IPartnershipInviteRepository, PartnershipInviteRepository>();
        services.AddScoped<IPartnerConnectionRepository, PartnerConnectionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
