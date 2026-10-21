namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a partnership connection cannot be found for the requesting company,
/// either because it does not exist or it belongs to other companies. Produces a 404.
/// </summary>
public sealed class PartnershipNotFoundException : Exception
{
    public PartnershipNotFoundException(Guid connectionId)
        : base($"Parceria '{connectionId}' não encontrada.")
    {
    }
}
