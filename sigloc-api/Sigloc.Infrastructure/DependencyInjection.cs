using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Repositories;
using System.Net.Http.Headers;
using Sigloc.Infrastructure.Authentication;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Notifications;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Infrastructure.Routing;

namespace Sigloc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<SiglocDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.Configure<InviteSettings>(configuration.GetSection(InviteSettings.SectionName));

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

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRouteSegmentRepository, RouteSegmentRepository>();
        services.AddScoped<IConsolidatedRouteRepository, ConsolidatedRouteRepository>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IAuctionNotifier, LoggingAuctionNotifier>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContractorRepository, ContractorRepository>();
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddScoped<IPartnershipInviteRepository, PartnershipInviteRepository>();
        services.AddScoped<IPartnerConnectionRepository, PartnerConnectionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
