namespace Sigloc.Application.Exceptions;
public sealed class TripNotFoundException(Guid id) : Exception($"Trip '{id}' was not found.");
