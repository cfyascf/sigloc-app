using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetValidByHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task InvalidateActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
