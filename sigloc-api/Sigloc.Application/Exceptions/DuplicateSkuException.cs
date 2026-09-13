namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a SKU already exists for the contractor. Produces a 409 response.
/// </summary>
public sealed class DuplicateSkuException : Exception
{
    public string Sku { get; }

    public DuplicateSkuException(string sku)
        : base($"Já existe um produto com o SKU '{sku}' cadastrado para este contratante.")
    {
        Sku = sku;
    }
}
