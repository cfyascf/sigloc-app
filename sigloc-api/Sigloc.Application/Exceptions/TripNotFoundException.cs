namespace Sigloc.Application.Exceptions;
public sealed class TripNotFoundException(Guid id) : Exception("Viagem não encontrada ou não pertence à sua empresa.")
{
    public Guid Id { get; } = id;
}
