using Microsoft.Extensions.DependencyInjection;
using Sigloc.Application.Contracts;
using Sigloc.Application.Services;

namespace Sigloc.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IRouteSegmentService, RouteSegmentService>();
        services.AddScoped<IRoutePreviewService, RoutePreviewService>();
        services.AddScoped<IAuctionService, AuctionService>();
        services.AddScoped<IFreightOfferService, FreightOfferService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartnerNetworkService, PartnerNetworkService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITripMonitoringService, TripMonitoringService>();

        return services;
    }
}
