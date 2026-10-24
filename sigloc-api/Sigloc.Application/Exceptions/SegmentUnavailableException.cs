namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a route segment cannot be consolidated because it is not available
/// (its status is not <c>Available</c> or it is already attached to a route).
/// Produces a 409 response.
/// </summary>
public sealed class SegmentUnavailableException : Exception
{
    public Guid SegmentId { get; }

    public SegmentUnavailableException(Guid segmentId)
        : base("Um dos trechos selecionados não está disponível: ele já faz parte de outra rota em leilão ou em andamento.")
    {
        SegmentId = segmentId;
    }
}
