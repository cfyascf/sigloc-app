using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sigloc.Application.Exceptions;

namespace Sigloc.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, payload) = exception switch
        {
            ValidationException validation => (
                (int)HttpStatusCode.BadRequest,
                (object)new
                {
                    error = "VALIDATION_ERROR",
                    message = validation.Message,
                    details = validation.Errors.Select(e => new { field = e.Field, reason = e.Reason })
                }),

            DuplicateSkuException duplicate => (
                (int)HttpStatusCode.Conflict,
                new
                {
                    error = "SKU_DUPLICADO",
                    message = duplicate.Message,
                    details = Array.Empty<object>()
                }),

            ProductNotFoundException notFound => (
                (int)HttpStatusCode.NotFound,
                new
                {
                    error = "PRODUTO_NAO_ENCONTRADO",
                    message = notFound.Message
                }),

            ProductInUseException inUse => (
                (int)HttpStatusCode.Conflict,
                new
                {
                    error = "PRODUTO_EM_USO",
                    message = inUse.Message,
                    trechosVinculados = inUse.LinkedSegments
                }),

            EmailAlreadyExistsException emailConflict => (
                (int)HttpStatusCode.Conflict,
                new
                {
                    error = "EMAIL_JA_EXISTE",
                    message = emailConflict.Message
                }),

            CnpjAlreadyExistsException cnpjConflict => (
                (int)HttpStatusCode.Conflict,
                new
                {
                    error = "CNPJ_JA_EXISTE",
                    message = cnpjConflict.Message
                }),

            CarrierAlreadyRegisteredException carrierConflict => (
                (int)HttpStatusCode.Conflict,
                new
                {
                    error = "TRANSPORTADORA_JA_CADASTRADA",
                    message = carrierConflict.Message,
                    redirecionarParaLogin = true
                }),

            InvalidInviteException invalidInvite => (
                (int)HttpStatusCode.BadRequest,
                new
                {
                    error = "CONVITE_INVALIDO",
                    message = invalidInvite.Message
                }),

            InvalidCredentialsException invalidCredentials => (
                (int)HttpStatusCode.Unauthorized,
                new
                {
                    error = "CREDENCIAIS_INVALIDAS",
                    message = invalidCredentials.Message
                }),

            InvalidGoogleTokenException invalidGoogle => (
                (int)HttpStatusCode.Unauthorized,
                new
                {
                    error = "TOKEN_GOOGLE_INVALIDO",
                    message = invalidGoogle.Message
                }),

            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                new { error = "NOT_FOUND", message = exception.Message }),

            ArgumentException => (
                (int)HttpStatusCode.BadRequest,
                new { error = "BAD_REQUEST", message = exception.Message }),

            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                new { error = "UNAUTHORIZED", message = exception.Message }),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                new { error = "INTERNAL_SERVER_ERROR", message = "Ocorreu um erro inesperado." })
        };

        context.Response.StatusCode = statusCode;

        var result = JsonSerializer.Serialize(payload, SerializerOptions);
        return context.Response.WriteAsync(result);
    }
}
