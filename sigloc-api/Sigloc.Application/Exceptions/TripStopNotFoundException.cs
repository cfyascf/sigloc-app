namespace Sigloc.Application.Exceptions;
public sealed class TripStopNotFoundException(Guid id) : Exception("Parada não encontrada nesta viagem.")
{
    public Guid Id { get; } = id;
}
