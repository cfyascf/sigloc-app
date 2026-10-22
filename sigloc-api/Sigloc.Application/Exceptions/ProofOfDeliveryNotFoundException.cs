namespace Sigloc.Application.Exceptions;
public sealed class ProofOfDeliveryNotFoundException(Guid id) : Exception($"Proof of delivery '{id}' was not found.");
