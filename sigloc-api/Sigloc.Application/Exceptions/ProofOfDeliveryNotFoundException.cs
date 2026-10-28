namespace Sigloc.Application.Exceptions;
public sealed class ProofOfDeliveryNotFoundException(Guid id) : Exception("Comprovante de entrega não encontrado.")
{
    public Guid Id { get; } = id;
}
