namespace Sigloc.Application.Exceptions;

public sealed class TrackingUnavailableException : Exception
{
    public TrackingUnavailableException() : base("Trip monitoring is temporarily unavailable.")
    {
    }
}
