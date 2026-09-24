namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a plate already exists for the carrier. Produces a 409 response.
/// </summary>
public sealed class DuplicatePlateException : Exception
{
    public string Plate { get; }

    public DuplicatePlateException(string plate)
        : base($"Já existe um veículo com a placa '{plate}' cadastrado para esta transportadora.")
    {
        Plate = plate;
    }
}
