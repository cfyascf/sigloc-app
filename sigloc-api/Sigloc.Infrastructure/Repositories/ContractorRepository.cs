using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class ContractorRepository : IContractorRepository
{
    private readonly SiglocDbContext _dbContext;

    public ContractorRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Contractor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Contractors
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> CnpjExistsAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        return _dbContext.Contractors
            .AsNoTracking()
            .AnyAsync(c => c.Cnpj == cnpj, cancellationToken);
    }

    public async Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default)
    {
        await _dbContext.Contractors.AddAsync(contractor, cancellationToken);
    }
}
