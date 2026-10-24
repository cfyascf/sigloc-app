namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when an auction does not exist or belongs to another contractor.
/// Produces a 404 response.
/// </summary>
public sealed class AuctionNotFoundException : Exception
{
    public AuctionNotFoundException(Guid id)
        : base("Leilão não encontrado ou não pertence a este contratante.")
    {
    }
}
