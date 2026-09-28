/**
 * Dashboard service — boundary between the UI and the Sigloc executive dashboard
 * endpoint.
 *
 * `GET /api/dashboard/executivo` is an aggregator (BFF) that returns a
 * `DashboardExecutivoDto` (KPIs, network efficiency, top cost deviations and the
 * most critical SLA milestones) already computed server-side, scoped to the
 * authenticated contractor.
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/dashboard"

/** Normalizes a `DashboardExecutivoDto` into a plain object for the UI. */
export function toExecutiveDashboard(dto) {
  if (!dto) {
    return null
  }

  const kpis = dto.kpis ?? {}
  const efficiency = dto.networkEfficiency ?? {}

  return {
    kpis: {
      unassignedSegments: kpis.unassignedSegments ?? 0,
      activeAuctions: kpis.activeAuctions ?? 0,
      inTransitTrips: kpis.inTransitTrips ?? 0,
      blockedOverbookings: kpis.blockedOverbookings ?? 0,
    },
    networkEfficiency: {
      averageWeightOccupationPercentage:
        efficiency.averageWeightOccupationPercentage ?? 0,
      averageVolumeOccupationPercentage:
        efficiency.averageVolumeOccupationPercentage ?? 0,
      routeUtilizationPercentage: efficiency.routeUtilizationPercentage ?? 0,
    },
    costDeviations: (dto.costDeviations ?? []).map((item) => ({
      routeId: item.routeId ?? "",
      itinerary: item.itinerary ?? "",
      targetBudget: item.targetBudget ?? 0,
      currentBestBid: item.currentBestBid ?? 0,
      deviationAmount: item.deviationAmount ?? 0,
      isOverBudget: Boolean(item.isOverBudget),
    })),
    slaMilestones: (dto.slaMilestones ?? []).map((item) => ({
      referenceCode: item.referenceCode ?? "",
      itinerary: item.itinerary ?? "",
      milestoneType: item.milestoneType ?? "",
      timeRemainingMinutes: item.timeRemainingMinutes ?? 0,
      isCritical: Boolean(item.isCritical),
    })),
  }
}

/** Fetches the executive dashboard for the authenticated contractor. */
export async function getExecutiveDashboard(options) {
  const payload = await apiClient.get(`${BASE}/executivo`, options)
  return toExecutiveDashboard(payload)
}

export const dashboardService = {
  getExecutiveDashboard,
  toExecutiveDashboard,
}
