namespace Sigloc.Application.Exceptions;

public sealed record ValidationError(string Field, string Reason);

/// <summary>
/// Raised when a request body fails business validation. Produces a 400 response.
/// </summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors, string? message = null)
        : base(message ?? "Não foi possível cadastrar o produto.")
    {
        Errors = errors;
    }
}
