namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the geocoding provider answered but found no location for the address. Unlike
/// <see cref="GeocodingException"/> (provider failure, 502) this is a problem with the user's
/// input, so callers report it as a validation error. Produces a 422 response if unhandled.
/// </summary>
public sealed class AddressNotFoundException : Exception
{
    public string Address { get; }

    public AddressNotFoundException(string address)
        : base($"Endereço não encontrado: '{address}'. Confira a cidade e a UF (ex.: Curitiba, PR).")
    {
        Address = address;
    }
}
