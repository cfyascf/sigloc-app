namespace Sigloc.Application.Exceptions;

/// <summary>Raised when a stop other than the first pending one is confirmed. Produces a 409 response.</summary>
public sealed class TripStopNotActionableException(Guid id)
    : Exception($"Somente a próxima parada pendente pode ser concluída (parada '{id}').");
