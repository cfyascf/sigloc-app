namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the authenticated user's company (contractor or carrier) cannot be
/// found, for example a JWT whose company no longer exists. Produces a 404 response.
/// </summary>
public sealed class CompanyNotFoundException : Exception
{
    public CompanyNotFoundException(Guid companyId)
        : base($"Empresa '{companyId}' não encontrada.")
    {
    }
}
