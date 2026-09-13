using Microsoft.Extensions.DependencyInjection;
using Sigloc.Application.Contracts;
using Sigloc.Application.Services;

namespace Sigloc.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
