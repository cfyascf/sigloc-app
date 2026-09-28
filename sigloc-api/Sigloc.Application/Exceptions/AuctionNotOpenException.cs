namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when an operation requires an open auction but the auction is already
/// closed or cancelled. Produces a 409 response.
/// </summary>
public sealed class AuctionNotOpenException : Exception
{
    public AuctionNotOpenException(Guid id)
        : base($"Auction {id} is not open and cannot be awarded.")
    {
    }
}
