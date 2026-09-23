namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when one or more requested route segment ids do not exist or do not belong
/// to the calling contractor. Produces a 404 response.
/// </summary>
public sealed class RouteSegmentsNotFoundException : Exception
{
    public IReadOnlyList<Guid> MissingIds { get; }

    public RouteSegmentsNotFoundException(IReadOnlyList<Guid> missingIds)
        : base($"The following route segments do not exist or do not belong to this contractor: {string.Join(", ", missingIds)}.")
    {
        MissingIds = missingIds;
    }
}
