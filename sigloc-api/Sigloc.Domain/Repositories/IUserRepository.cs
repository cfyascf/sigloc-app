using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Stages a new user. Persisted when the unit of work is committed.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
