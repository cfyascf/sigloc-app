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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<RouteSegmentAggregator>();
        services.AddScoped<IRoutePreviewService, RoutePreviewService>();
        services.AddScoped<IAuctionService, AuctionService>();

        return services;
    }
}
