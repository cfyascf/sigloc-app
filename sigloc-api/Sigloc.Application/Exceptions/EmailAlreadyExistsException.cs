namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the e-mail is already registered. Produces a 409 response.
/// </summary>
public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"O e-mail '{email}' já está em uso.")
    {
    }
}
