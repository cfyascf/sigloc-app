namespace Sigloc.Application.Exceptions;
public sealed class TripStopNotFoundException(Guid id) : Exception($"Trip stop '{id}' was not found.");
