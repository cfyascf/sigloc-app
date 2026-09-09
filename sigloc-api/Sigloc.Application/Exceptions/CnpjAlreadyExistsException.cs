namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the CNPJ is already registered. Produces a 409 response.
/// </summary>
public sealed class CnpjAlreadyExistsException : Exception
{
    public CnpjAlreadyExistsException(string cnpj)
        : base($"O CNPJ '{cnpj}' já está em uso.")
    {
    }
}
