namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when one or more requested route segments do not exist or do not belong to
/// the current contractor. Produces a 404 response.
/// </summary>
public sealed class RouteSegmentsNotFoundException : Exception
{
    public IReadOnlyList<Guid> MissingIds { get; }

    public RouteSegmentsNotFoundException(IReadOnlyList<Guid> missingIds)
        : base($"{missingIds.Count} route segment(s) do not exist or do not belong to this contractor.")
    {
        MissingIds = missingIds;
    }
}
