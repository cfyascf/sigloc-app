using Sigloc.Api.Extensions;
using Sigloc.Api.Middleware;
using Sigloc.Application;
using Sigloc.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Microsoft.OpenApi; // Required for logging
using Sigloc.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting FreightGuard Web API...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddEndpointsApiExplorer();

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

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes.Add("Bearer", jwtScheme);

            return Task.CompletedTask;
        });
    });

    builder.Services.AddSiglocAuthentication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddControllers();

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

    // In Development, apply pending migrations and seed sample data on startup so a
    // freshly started local database (e.g. the Docker Compose Postgres container) is
    // immediately ready for testing.
    if (app.Environment.IsDevelopment())
    {
        await Sigloc.Infrastructure.Persistence.DatabaseSeeder.MigrateAndSeedAsync(app.Services);
    }

    // Catch unhandled exceptions and return a consistent JSON error payload
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Log all incoming HTTP requests automatically
    app.UseSerilogRequestLogging();

    // Generate the openapi.json file
    app.MapOpenApi();
    
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
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}