namespace Sigloc.Domain.Repositories;

using Sigloc.Domain.Entities;

public interface ITelemetryRepository
{
    Task<IEnumerable<Telemetry>> GetHistoryByTravelIdAsync(Guid travelId);
    Task AddAsync(Telemetry telemetry);
}