using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class CarrierRepository : ICarrierRepository
{
    private readonly SiglocDbContext _dbContext;

    public CarrierRepository(SiglocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Carrier?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        return _dbContext.Carriers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Cnpj == cnpj, cancellationToken);
    }

    public Task<Carrier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Carriers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<Carrier?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Carriers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> CnpjExistsAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        return _dbContext.Carriers
            .AsNoTracking()
            .AnyAsync(c => c.Cnpj == cnpj, cancellationToken);
    }

    public async Task AddAsync(Carrier carrier, CancellationToken cancellationToken = default)
    {
        await _dbContext.Carriers.AddAsync(carrier, cancellationToken);
    }

    public async Task UpdateAsync(Carrier carrier, CancellationToken cancellationToken = default)
    {
        _dbContext.Carriers.Update(carrier);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
