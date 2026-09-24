namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a vehicle does not exist or belongs to another carrier. Produces a 404 response.
/// </summary>
public sealed class VehicleNotFoundException : Exception
{
    public VehicleNotFoundException(Guid id)
        : base($"Veículo {id} não existe ou não pertence a esta transportadora.")
    {
    }
}
