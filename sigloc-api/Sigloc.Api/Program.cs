using Sigloc.Api.Extensions;
using Sigloc.Api.Middleware;
using Sigloc.Application;
using Sigloc.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
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
                    "http://localhost:5173", // Vite default local port
                    "https://icy-flower-092597810.7.azurestaticapps.net" // Your live frontend
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();


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
