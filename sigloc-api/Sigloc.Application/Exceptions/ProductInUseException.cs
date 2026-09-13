namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a product cannot be deleted because it is linked to route segments (trechos).
/// Produces a 409 response.
/// </summary>
public sealed class ProductInUseException : Exception
{
    public IReadOnlyList<string> LinkedSegments { get; }

    public ProductInUseException(IReadOnlyList<string> linkedSegments)
        : base($"Este produto está vinculado a {linkedSegments.Count} trecho(s) e não pode ser excluído.")
    {
        LinkedSegments = linkedSegments;
    }
}
