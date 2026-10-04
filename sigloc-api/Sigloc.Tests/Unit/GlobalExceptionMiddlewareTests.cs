using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Sigloc.Api.Middleware;
using Sigloc.Application.Exceptions;

namespace Sigloc.Tests.Unit;

public class GlobalExceptionMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> RunAsync(Exception thrown)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw thrown;
        var middleware = new GlobalExceptionHandlerMiddleware(
            next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var doc = JsonDocument.Parse(json);
        return (context.Response.StatusCode, doc.RootElement.Clone());
    }

    [Fact]
    public async Task Passes_through_when_no_exception()
    {
        var context = new DefaultHttpContext();
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };
        var middleware = new GlobalExceptionHandlerMiddleware(
            next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
    }

    public static IEnumerable<object[]> ExceptionCases()
    {
        yield return new object[] { new TripNotFoundException(Guid.NewGuid()), 404, "TRIP_NOT_FOUND" };
        yield return new object[] { new TrackingUnavailableException(), 503, "TRACKING_UNAVAILABLE" };
        yield return new object[] { new ValidationException(new[] { new ValidationError("f", "r") }), 400, "VALIDATION_ERROR" };
        yield return new object[] { new DuplicateSkuException("SKU"), 409, "SKU_DUPLICADO" };
        yield return new object[] { new ProductNotFoundException(Guid.NewGuid()), 404, "PRODUTO_NAO_ENCONTRADO" };
        yield return new object[] { new ProductInUseException(new[] { "seg" }), 409, "PRODUTO_EM_USO" };
        yield return new object[] { new RouteSegmentNotFoundException(Guid.NewGuid()), 404, "ROUTE_SEGMENT_NOT_FOUND" };
        yield return new object[] { new AuctionNotFoundException(Guid.NewGuid()), 404, "AUCTION_NOT_FOUND" };
        yield return new object[] { new RouteSegmentNotEditableException(Guid.NewGuid()), 409, "ROUTE_SEGMENT_NOT_EDITABLE" };
        yield return new object[] { new RouteSegmentsNotFoundException(new[] { Guid.NewGuid() }), 404, "ROUTE_SEGMENTS_NOT_FOUND" };
        yield return new object[] { new SegmentUnavailableException(Guid.NewGuid()), 409, "SEGMENT_UNAVAILABLE" };
        yield return new object[] { new GeocodingException("boom"), 502, "GEOCODING_ERROR" };
        yield return new object[] { new EmailAlreadyExistsException("a@b.com"), 409, "EMAIL_JA_EXISTE" };
        yield return new object[] { new CnpjAlreadyExistsException("123"), 409, "CNPJ_JA_EXISTE" };
        yield return new object[] { new CarrierAlreadyRegisteredException("123"), 409, "TRANSPORTADORA_JA_CADASTRADA" };
        yield return new object[] { new ContractorNotFoundException(Guid.NewGuid()), 404, "CONTRATANTE_NAO_ENCONTRADO" };
        yield return new object[] { new InvalidInviteException(), 400, "CONVITE_INVALIDO" };
        yield return new object[] { new PartnershipAlreadyExistsException(), 409, "PARCERIA_JA_EXISTE" };
        yield return new object[] { new InvalidCredentialsException(), 401, "CREDENCIAIS_INVALIDAS" };
        yield return new object[] { new InvalidGoogleTokenException(), 401, "TOKEN_GOOGLE_INVALIDO" };
        yield return new object[] { new InvalidPasswordResetTokenException(), 400, "PASSWORD_RESET_TOKEN_INVALIDO" };
        yield return new object[] { new BidNotFoundException(Guid.NewGuid()), 404, "BID_NOT_FOUND" };
        yield return new object[] { new AuctionNotOpenException(Guid.NewGuid()), 409, "AUCTION_NOT_OPEN" };
        yield return new object[] { new BidRejectedException(BidRejectionCode.AboveCeiling, "nope"), 400, "AboveCeiling" };
        yield return new object[] { new OfferNotFoundException(Guid.NewGuid()), 404, "OFFER_NOT_FOUND" };
        yield return new object[] { new KeyNotFoundException("missing"), 404, "NOT_FOUND" };
        yield return new object[] { new ArgumentException("bad"), 400, "BAD_REQUEST" };
        yield return new object[] { new UnauthorizedAccessException("no"), 401, "UNAUTHORIZED" };
        yield return new object[] { new InvalidOperationException("unexpected"), 500, "INTERNAL_SERVER_ERROR" };
    }

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public async Task Maps_exception_to_status_and_error_code(Exception thrown, int expectedStatus, string expectedError)
    {
        var (status, body) = await RunAsync(thrown);

        status.Should().Be(expectedStatus);
        body.GetProperty("error").GetString().Should().Be(expectedError);
    }

    [Fact]
    public async Task Response_content_type_is_json()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => throw new InvalidOperationException();
        var middleware = new GlobalExceptionHandlerMiddleware(
            next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task Validation_error_includes_details()
    {
        var (_, body) = await RunAsync(new ValidationException(new[] { new ValidationError("campo", "obrigatório") }));

        var details = body.GetProperty("details");
        details.GetArrayLength().Should().Be(1);
        details[0].GetProperty("field").GetString().Should().Be("campo");
        details[0].GetProperty("reason").GetString().Should().Be("obrigatório");
    }

    [Fact]
    public async Task Internal_error_hides_original_message()
    {
        var (_, body) = await RunAsync(new InvalidOperationException("secret internal detail"));

        body.GetProperty("message").GetString().Should().NotContain("secret internal detail");
    }
}
