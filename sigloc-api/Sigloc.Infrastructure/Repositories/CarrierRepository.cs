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
}
