namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a vehicle plate is already registered. Produces a 409 response.
/// </summary>
public sealed class DuplicatePlateException : Exception
{
    public string Plate { get; }

    public DuplicatePlateException(string plate)
        : base($"Já existe um veículo cadastrado com a placa {plate}.")
    {
        Plate = plate;
    }
}
