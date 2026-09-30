namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a freight offer (auction) does not exist, is closed/expired, or is not
/// visible to the carrier because there is no active partnership with the owning
/// contractor. Produces a 404 response.
/// </summary>
public sealed class OfferNotFoundException : Exception
{
    public string ResourceType => "offer";

    public Guid ResourceId { get; }

    public OfferNotFoundException(Guid id)
        : base($"Offer {id} not found.")
    {
        ResourceId = id;
    }
}
