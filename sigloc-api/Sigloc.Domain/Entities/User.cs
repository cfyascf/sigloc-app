using System.ComponentModel.DataAnnotations.Schema;
using Sigloc.Domain.Enums;

namespace Sigloc.Domain.Entities;

/// <summary>
/// Access credentials (USUARIO). Isolated from the public company data so login
/// concerns are separated from company concerns.
/// </summary>
[Table("User")]
public class User : BaseEntity
{
    /// <summary>Login credential. Unique across the platform.</summary>
    public required string Email { get; set; }

    /// <summary>Cryptographic hash of the password (BCrypt). Never stored in plain text.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Access profile driving RBAC.</summary>
    public ProfileType ProfileType { get; set; }

    /// <summary>
    /// Owning company id. Required for Contratante and Transportador (points to the
    /// matching Contractor/Carrier row), null for Admin.
    /// </summary>
    public Guid? CompanyId { get; set; }
}
