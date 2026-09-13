using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class PartnershipInviteRepository : IPartnershipInviteRepository
{
    private readonly SiglocDbContext _dbContext;

    public PartnershipInviteRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PartnershipInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return _dbContext.PartnershipInvites
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task AddAsync(PartnershipInvite invite, CancellationToken cancellationToken = default)
    {
        await _dbContext.PartnershipInvites.AddAsync(invite, cancellationToken);
    }
}
