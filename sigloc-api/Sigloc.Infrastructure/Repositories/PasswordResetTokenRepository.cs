using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly SiglocDbContext _dbContext;

    public PasswordResetTokenRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        => _dbContext.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();

    public Task<PasswordResetToken?> GetValidByHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
        => _dbContext.PasswordResetTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash && token.UsedAt == null && token.ExpiresAt > now,
            cancellationToken);

    public async Task InvalidateActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.UsedAt = now;
        }
    }
}
