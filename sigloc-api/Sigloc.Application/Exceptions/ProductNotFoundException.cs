namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a product does not exist or belongs to another contractor. Produces a 404 response.
/// </summary>
public sealed class ProductNotFoundException : Exception
{
    public string ResourceType => "product";

    public Guid ResourceId { get; }

    public ProductNotFoundException(Guid id)
        : base($"Produto {id} não existe ou não pertence a este contratante.")
    {
        ResourceId = id;
    }
}
