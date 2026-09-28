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

    /// <summary>
    /// Builds the carrier (transportador) dashboard for the fleet owner: immediate KPI counters,
    /// monthly performance metrics, the active-bid radar (competitiveness vs. the current leader)
    /// and the most critical SLA milestones. The independent aggregations run in parallel and the
    /// result is returned ready to render, scoped to the calling carrier.
    /// </summary>
    Task<DashboardTransportadorDto> GetCarrierDashboardAsync(Guid carrierId, CancellationToken cancellationToken = default);
}
