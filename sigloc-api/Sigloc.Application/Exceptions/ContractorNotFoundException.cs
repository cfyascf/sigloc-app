namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when an operation references a contractor that does not exist (for example
/// a JWT whose company no longer exists). Produces a 404 response.
/// </summary>
public sealed class ContractorNotFoundException : Exception
{
    public ContractorNotFoundException(Guid contractorId)
        : base($"Contratante '{contractorId}' não encontrado.")
    {
    }
}
