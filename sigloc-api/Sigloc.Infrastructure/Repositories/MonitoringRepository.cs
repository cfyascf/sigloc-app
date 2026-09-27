using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class MonitoringRepository : IMonitoringRepository
{
    private readonly SiglocDbContext _context;

    public MonitoringRepository(SiglocDbContext context)
    {
        _context = context;
    }

    public async Task<Monitoring?> GetByTravelIdAsync(Guid travelId)
    {
        return await _context.Monitorings
            .FirstOrDefaultAsync(m => m.ViagemId == travelId);
    }

    public async Task AddAsync(Monitoring monitoring)
    {
        await _context.Monitorings.AddAsync(monitoring);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Monitoring monitoring)
    {
        _context.Monitorings.Update(monitoring);
        await _context.SaveChangesAsync();
    }
}