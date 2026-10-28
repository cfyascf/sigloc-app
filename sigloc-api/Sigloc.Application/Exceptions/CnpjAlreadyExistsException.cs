namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the CNPJ is already registered. Produces a 409 response.
/// </summary>
public sealed class CnpjAlreadyExistsException : Exception
{
    public CnpjAlreadyExistsException(string cnpj)
        : base($"O CNPJ {Format(cnpj)} já está em uso.")
    {
    }

    // 00.000.000/0000-00 when the value has the 14 digits, as typed otherwise.
    private static string Format(string cnpj)
        => cnpj.Length == 14 && cnpj.All(char.IsDigit)
            ? $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}"
            : cnpj;
}
