namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a route segment does not exist or belongs to another contractor.
/// Produces a 404 response.
/// </summary>
public sealed class RouteSegmentNotFoundException : Exception
{
    public RouteSegmentNotFoundException(Guid id)
        : base($"Route segment {id} does not exist or does not belong to this contractor.")
    {
    }
}
