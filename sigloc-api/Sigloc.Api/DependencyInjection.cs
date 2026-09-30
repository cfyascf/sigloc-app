using System.Text;
using System.Text.Json;
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

                // Return the standardized JSON error body (same shape as the global
                // exception middleware) instead of an empty 401/403 response.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "UNAUTHORIZED", "Authentication is required to access this resource.");
                    },
                    OnForbidden = context => WriteErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "FORBIDDEN", "Access denied")
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

    private static Task WriteErrorAsync(HttpContext httpContext, int statusCode, string error, string message)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(new { error, message }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
