using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class PartnerConnectionRepository : IPartnerConnectionRepository
{
    private readonly SiglocDbContext _dbContext;

    public PartnerConnectionRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid contractorId, Guid carrierId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PartnerConnections
            .AsNoTracking()
            .AnyAsync(c => c.ContractorId == contractorId && c.CarrierId == carrierId, cancellationToken);
    }

    public async Task AddAsync(PartnerConnection connection, CancellationToken cancellationToken = default)
    {
        await _dbContext.PartnerConnections.AddAsync(connection, cancellationToken);
    }
}
