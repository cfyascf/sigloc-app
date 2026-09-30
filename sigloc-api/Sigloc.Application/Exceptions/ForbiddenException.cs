namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the caller is authenticated but not allowed to access a resource.
/// Produces a 403 response.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public string? ResourceType { get; }

    public Guid? ResourceId { get; }

    public ForbiddenException(string message = "Access denied", string? resourceType = null, Guid? resourceId = null)
        : base(message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
