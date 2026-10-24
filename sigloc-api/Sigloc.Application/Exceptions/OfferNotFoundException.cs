namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a freight offer (auction) does not exist, is closed/expired, or is not
/// visible to the carrier because there is no active partnership with the owning
/// contractor. Produces a 404 response.
/// </summary>
public sealed class OfferNotFoundException : Exception
{
    public Guid OfferId { get; }

    public OfferNotFoundException(Guid id)
        : base("Oferta não encontrada ou não está mais disponível.")
    {
        OfferId = id;
    }
}
