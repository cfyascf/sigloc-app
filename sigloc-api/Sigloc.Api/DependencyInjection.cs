using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sigloc.Domain.Constants;
using Sigloc.Infrastructure.Authentication;

namespace Sigloc.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddSiglocAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException($"Configuration section '{JwtSettings.SectionName}' is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
            });

        services.AddAuthorization(options =>
        {
            // Admins can do anything a Shipper can do
            options.AddPolicy(Policies.RequireShipperAccess, policy => 
                policy.RequireRole(Roles.Shipper, Roles.Admin));

            // Admins can do anything a Carrier can do
            options.AddPolicy(Policies.RequireCarrierAccess, policy => 
                policy.RequireRole(Roles.Carrier, Roles.Admin));

            // Only Admins can access Admin-level endpoints
            options.AddPolicy(Policies.RequireAdminAccess, policy => 
                policy.RequireRole(Roles.Admin));
        });

        return services;
    }
}
