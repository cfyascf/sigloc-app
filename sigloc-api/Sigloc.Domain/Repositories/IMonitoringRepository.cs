using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface IMonitoringRepository
{
    Task<Monitoring?> GetByTravelIdAsync(Guid travelId);
    Task AddAsync(Monitoring monitoring);
    Task UpdateAsync(Monitoring monitoring);
}