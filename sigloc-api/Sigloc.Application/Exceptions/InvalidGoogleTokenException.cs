namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a Google ID token is missing, malformed or fails verification.
/// Produces a 401 response.
/// </summary>
public sealed class InvalidGoogleTokenException : Exception
{
    public InvalidGoogleTokenException(Exception? innerException = null)
        : base("O token do Google é inválido ou expirou.", innerException)
    {
    }
}
