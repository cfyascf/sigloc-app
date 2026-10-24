namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a position refresh is requested for a trip whose vehicle has no tracking device.
/// Unlike <see cref="TrackingUnavailableException"/> this is not transient: retrying cannot help
/// until a device is registered. Produces a 422 response.
/// </summary>
public sealed class VehicleWithoutTrackerException : Exception
{
    public VehicleWithoutTrackerException()
        : base("Este veículo não tem um rastreador cadastrado. Informe o ID do dispositivo Traccar em Gestão de Frota para transmitir a posição.")
    {
    }
}
