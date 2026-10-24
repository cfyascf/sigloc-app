namespace Sigloc.Application.Exceptions;

public sealed class TrackingUnavailableException : Exception
{
    public TrackingUnavailableException() : base("O monitoramento da viagem está temporariamente indisponível. Tente novamente em instantes.")
    {
    }
}
