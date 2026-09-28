/**
 * Dashboard service — boundary between the UI and the Sigloc dashboard endpoints.
 *
 * `GET /api/dashboard/executivo` returns a `DashboardExecutivoDto` for the contractor
 * (KPIs, network efficiency, top cost deviations and the most critical SLA milestones).
 *
 * `GET /api/dashboard/transportador` returns a `DashboardTransportadorDto` for the carrier
 * (operational KPIs, monthly performance, the active-bid radar and the SLA control tower).
 *
 * Both are aggregators (BFF) computed server-side and scoped to the authenticated company.
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

/** Normalizes a `DashboardTransportadorDto` into a plain object for the UI. */
export function toCarrierDashboard(dto) {
  if (!dto) {
    return null
  }

  const kpis = dto.kpis ?? {}
  const performance = dto.performance ?? {}

  return {
    kpis: {
      availableVehicles: kpis.availableVehicles ?? 0,
      activeBids: kpis.activeBids ?? 0,
      inTransitTrips: kpis.inTransitTrips ?? 0,
    },
    performance: {
      fleetOperationPercentage: performance.fleetOperationPercentage ?? 0,
      capacityUtilizationPercentage:
        performance.capacityUtilizationPercentage ?? 0,
      auctionSuccessRate: performance.auctionSuccessRate ?? 0,
    },
    activeDisputes: (dto.activeDisputes ?? []).map((item) => ({
      routeId: item.routeId ?? "",
      itinerary: item.itinerary ?? "",
      status: item.status ?? "",
      myBidAmount: item.myBidAmount ?? 0,
      leaderBidAmount: item.leaderBidAmount ?? 0,
      amountToCover: item.amountToCover ?? 0,
    })),
    controlTower: (dto.controlTower ?? []).map((item) => ({
      vehiclePlate: item.vehiclePlate ?? "",
      referenceCode: item.referenceCode ?? "",
      milestoneType: item.milestoneType ?? "",
      timeRemainingMinutes: item.timeRemainingMinutes ?? 0,
      isDelayed: Boolean(item.isDelayed),
    })),
  }
}

/** Fetches the carrier (transportador) dashboard for the authenticated carrier. */
export async function getCarrierDashboard(options) {
  const payload = await apiClient.get(`${BASE}/transportador`, options)
  return toCarrierDashboard(payload)
}

export const dashboardService = {
  getExecutiveDashboard,
  toExecutiveDashboard,
  getCarrierDashboard,
  toCarrierDashboard,
}
