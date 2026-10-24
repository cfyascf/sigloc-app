namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when an operation requires an open auction but the auction is already
/// closed, cancelled or awarded. Produces a 409 response.
/// </summary>
public sealed class AuctionNotOpenException : Exception
{
    public Guid AuctionId { get; }

    public AuctionNotOpenException(Guid id)
        : base("Este leilão já foi encerrado, cancelado ou adjudicado.")
    {
        AuctionId = id;
    }
}
