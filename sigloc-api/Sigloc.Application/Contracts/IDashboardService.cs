using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

public interface IDashboardService
{
    /// <summary>
    /// Builds the executive dashboard (Dashboard Executivo) for the contractor: KPI counters,
    /// network efficiency, top cost deviations and the most critical SLA milestones. The
    /// independent aggregations run in parallel and the result is returned ready to render.
    /// </summary>
    Task<DashboardExecutivoDto> GetExecutiveDashboardAsync(Guid contractorId, CancellationToken cancellationToken = default);
}
