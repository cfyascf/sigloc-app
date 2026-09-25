/**
 * Route service — boundary between the UI and the Sigloc consolidated-route
 * endpoints. Currently exposes the real-time route preview simulation used when
 * advancing selected segments to the auction workspace.
 *
 * `POST /api/routes/preview` persists nothing; it sums the per-segment distances
 * and durations with the inter-segment legs and returns aggregated commercial
 * and physical metrics (`RoutePreviewResponseDto`).
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/routes"

/** Normalizes a `RoutePreviewResponseDto` into a plain object for the UI. */
export function toRoutePreview(dto) {
  if (!dto) {
    return null
  }

  const totals = dto.aggregatedTotals ?? {}
  const requirement = dto.consolidatedVehicleRequirement ?? {}

  return {
    totalDistanceKm: dto.totalDistanceKm ?? 0,
    estimatedTimeHours: dto.estimatedTimeHours ?? 0,
    consolidatedCeiling: dto.consolidatedCeiling ?? 0,
    estimatedAnttFloor: dto.estimatedAnttFloor ?? 0,
    estimatedToll: dto.estimatedToll ?? 0,
    costPerKm: dto.costPerKm ?? 0,
    aggregatedTotals: {
      totalWeightKg: totals.totalWeightKg ?? 0,
      totalVolumeM3: totals.totalVolumeM3 ?? 0,
    },
    consolidatedVehicleRequirement: {
      baseBodyworkType: requirement.baseBodyworkType ?? "",
      minRefrigerationLevel: requirement.minRefrigerationLevel ?? "",
      requiresMopp: Boolean(requirement.requiresMopp),
      requiresCargoFixing: Boolean(requirement.requiresCargoFixing),
    },
  }
}

/**
 * Simulates the consolidated route for the given segment ids.
 * @param {string[]} segmentIds
 */
export async function previewRoute(segmentIds, options) {
  const payload = await apiClient.post(
    `${BASE}/preview`,
    { segmentIds: segmentIds ?? [] },
    options
  )
  return toRoutePreview(payload)
}

export const routeService = {
  previewRoute,
  toRoutePreview,
}
