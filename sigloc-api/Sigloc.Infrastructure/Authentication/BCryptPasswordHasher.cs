using Sigloc.Application.Contracts;

namespace Sigloc.Infrastructure.Authentication;

/// <summary>
/// BCrypt-based password hasher. Passwords are salted and hashed; only the hash
/// is ever persisted.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must never crash the login flow.
            return false;
        }
    }
}
