namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when the routing provider works but finds no road route between two points (typically a
/// coordinate far from any road). Unlike <see cref="GeocodingException"/> (provider failure, 502)
/// this is a problem with what the user entered, so it is reported on the form.
/// </summary>
public sealed class RouteNotFoundException : Exception
{
    public RouteNotFoundException()
        : base("Não foi possível traçar uma rota rodoviária entre a origem e o destino. Confira se os pontos ficam perto de uma estrada.")
    {
    }
}
