namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when creating an auction and one of the requested route segments is not
/// <c>Available</c> (it is already linked to another consolidated route, in transit or
/// completed). Produces a 409 response (TRECHO_INDISPONIVEL).
/// </summary>
public sealed class RouteSegmentUnavailableException : Exception
{
    public Guid SegmentId { get; }

    public RouteSegmentUnavailableException(Guid segmentId)
        : base($"O trecho {segmentId} já está vinculado a outra rota em andamento.")
    {
        SegmentId = segmentId;
    }
}
