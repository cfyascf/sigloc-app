namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a vehicle does not exist or belongs to another company.
/// Produces a 404 response.
/// </summary>
public sealed class VehicleNotFoundException : Exception
{
    public string ResourceType => "vehicle";

    public Guid ResourceId { get; }

    public VehicleNotFoundException(Guid id)
        : base($"Vehicle {id} not found.")
    {
        ResourceId = id;
    }
}
