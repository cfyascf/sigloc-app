using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

[Table("PasswordResetToken")]
public class PasswordResetToken : BaseEntity
{
    public required Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
