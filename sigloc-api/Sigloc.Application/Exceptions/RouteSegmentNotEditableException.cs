namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a route segment cannot be edited or deleted because its status is not
/// <c>Available</c> (it is already attached to a consolidated route). Produces a 409 response.
/// </summary>
public sealed class RouteSegmentNotEditableException : Exception
{
    public RouteSegmentNotEditableException(Guid id)
        : base($"Route segment {id} is attached to a consolidated route. Remove it from the route before editing or deleting it.")
    {
    }
}
