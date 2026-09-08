using Sigloc.Api.Extensions;
using Scalar.AspNetCore;
using Serilog;
using Microsoft.OpenApi; // Required for logging
using Sigloc.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

//abuble

try
{
    Log.Information("Starting FreightGuard Web API...");
    var builder = WebApplication.CreateBuilder(args);

    // 2. Tell the builder to use Serilog using your appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddEndpointsApiExplorer();

    // 3. Modern Native OpenAPI (The .NET 10 Way)
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Sigloc API",
                Version = "v1",
                Description = "Modern Logistics B2B Engine"
            };
            return Task.CompletedTask;
        });

        // Add JWT Bearer Security Scheme
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var jwtScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Name = "Authorization",
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT"
            };

            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes.Add("Bearer", jwtScheme);

            return Task.CompletedTask;
        });
    });

    // 4. Custom Auth & Controllers
    builder.Services.AddSiglocAuthentication(builder.Configuration);
    
    builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Força a API a retornar (e aceitar) textos no lugar de números para os Enums
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    // 5. CORS setup for your React Frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5100", // Vite default local port
                    "https://icy-flower-092597810.7.azurestaticapps.net" // Your live frontend
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    builder.Services.AddDbContext<Sigloc.Infrastructure.Contexts.SiglocDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // 1. Provedor de JWT (que já havíamos colocado)
    builder.Services.AddScoped<Sigloc.Application.Contracts.IJwtProvider, Sigloc.Infrastructure.Authentication.JwtProvider>();

    // 2. Registro do Repositório (O serviço precisa do repositório para falar com o banco)
    builder.Services.AddScoped<Sigloc.Domain.Repositories.IVehicleRepository, Sigloc.Infrastructure.Repositories.VehicleRepository>();

    // 3. Registro do Serviço (A peça que estava faltando para o Controller)
    builder.Services.AddScoped<Sigloc.Application.Contracts.IVehicleService, Sigloc.Application.Services.VehicleService>();

    var app = builder.Build();

    // --- MIDDLEWARE PIPELINE ---
    
    // Log all incoming HTTP requests automatically
    app.UseSerilogRequestLogging();

    // Generate the openapi.json file
    app.MapOpenApi();
    
    // Map the beautiful Scalar UI to /scalar/v1
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Sigloc API Hub")
               .WithTheme(ScalarTheme.Mars) // Dark mode theme
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseCors("AllowFrontend");

    // Authentication must come BEFORE Authorization
    app.UseAuthentication(); 
    app.UseAuthorization();

    app.MapControllers();
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}