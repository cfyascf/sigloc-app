namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a bid does not exist or does not belong to the given auction.
/// Produces a 404 response.
/// </summary>
public sealed class BidNotFoundException : Exception
{
    public string ResourceType => "bid";

    public Guid ResourceId { get; }

    public BidNotFoundException(Guid id)
        : base($"Bid {id} does not exist or does not belong to this auction.")
    {
        ResourceId = id;
    }
}
