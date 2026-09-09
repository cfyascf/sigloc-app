namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when login credentials are invalid. Deliberately generic so it never
/// reveals whether the e-mail or the password was wrong. Produces a 401 response.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("E-mail ou senha inválidos.")
    {
    }
}
