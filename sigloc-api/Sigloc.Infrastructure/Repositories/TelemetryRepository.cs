using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class TelemetryRepository : ITelemetryRepository
{
    private readonly SiglocDbContext _context;

    public TelemetryRepository(SiglocDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Telemetry>> GetHistoryByTravelIdAsync(Guid travelId)
    {
        return await _context.Telemetries
            .Where(t => t.ViagemId == travelId)
            .OrderByDescending(t => t.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Telemetry telemetry)
    {
        await _context.Telemetries.AddAsync(telemetry);
        await _context.SaveChangesAsync();
    }
}