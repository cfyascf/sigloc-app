namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when an invite token cannot be used to register (missing, expired or
/// already consumed). Produces a 400 response.
/// </summary>
public sealed class InvalidInviteException : Exception
{
    public InvalidInviteException(string? message = null)
        : base(message ?? "O convite é inválido ou expirou.")
    {
    }
}
