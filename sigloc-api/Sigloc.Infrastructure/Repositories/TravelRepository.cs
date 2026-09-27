using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

public class TravelRepository : ITravelRepository
{
    private readonly SiglocDbContext _context;

    public TravelRepository(SiglocDbContext context)
    {
        _context = context;
    }

    public async Task<Travel?> GetByIdAsync(Guid id)
    {
        return await _context.Travels.FindAsync(id);
    }

    public async Task<IEnumerable<Travel>> GetAllActiveAsync()
    {
        // Traz as viagens em andamento já com o cache de monitoramento preenchido
        return await _context.Travels
            .Include(t => t.Monitoramento)
            .Where(t => t.Status == TravelStatus.EM_CURSO || t.Status == TravelStatus.ATRASADO)
            .AsNoTracking() // Melhora a performance para listagens de leitura
            .ToListAsync();
    }

    public async Task<Travel?> GetByIdWithMonitoringAsync(Guid id)
    {
        return await _context.Travels
            .Include(t => t.Monitoramento)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(Travel travel)
    {
        await _context.Travels.AddAsync(travel);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Travel travel)
    {
        _context.Travels.Update(travel);
        await _context.SaveChangesAsync();
    }
}