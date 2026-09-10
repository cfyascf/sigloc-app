namespace Sigloc.Application.Contracts;

/// <summary>
/// Abstracts the cryptographic password hashing so plain-text passwords never
/// leave the service layer.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a cryptographic hash for the supplied plain-text password.</summary>
    string Hash(string password);

    /// <summary>Verifies a plain-text password against a stored hash.</summary>
    bool Verify(string password, string passwordHash);
}
