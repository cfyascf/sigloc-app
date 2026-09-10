namespace Sigloc.Domain.Enums;

/// <summary>
/// How a user authenticates. Determines whether a local password hash is used or
/// the identity is delegated to an external provider.
/// </summary>
public enum AuthProvider
{
    /// <summary>Local e-mail + password (BCrypt hash stored).</summary>
    Local,

    /// <summary>Google Sign-In. No password stored; identity verified via Google ID token.</summary>
    Google
}
