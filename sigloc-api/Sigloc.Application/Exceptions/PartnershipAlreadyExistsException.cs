namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a carrier tries to connect through an invite but a partnership
/// with the inviting contractor already exists. Produces a 409 response.
/// </summary>
public sealed class PartnershipAlreadyExistsException : Exception
{
    public PartnershipAlreadyExistsException(string? contractorName = null)
        : base(contractorName is null
            ? "Você já possui uma parceria com este contratante."
            : $"Você já possui uma parceria com {contractorName}.")
    {
    }
}
