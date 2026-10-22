namespace Sigloc.Application.Exceptions;

/// <summary>Raised when an operational action targets a delivered or cancelled trip. Produces a 409 response.</summary>
public sealed class TripNotActiveException(Guid id)
    : Exception($"A viagem '{id}' já foi encerrada e não aceita novas ações.");
