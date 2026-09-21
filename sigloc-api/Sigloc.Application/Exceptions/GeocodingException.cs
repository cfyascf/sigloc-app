namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the external routing/geocoding provider cannot resolve an address or
/// compute the route. Produces a 502 response.
/// </summary>
public sealed class GeocodingException : Exception
{
    public GeocodingException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
