/**
 * Auction service — boundary between the UI and the Sigloc auction endpoints.
 *
 * `POST /api/auctions` consolidates the selected segments into a route and opens
 * its auction in a single call (`CreateAuctionRequestDto` →
 * `CreateAuctionResponseDto`).
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/auctions"

function toIso(value) {
  if (!value) {
    return null
  }

  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

/** Normalizes a `CreateAuctionResponseDto` into a plain object for the UI. */
export function toCreateAuctionResult(dto) {
  if (!dto) {
    return null
  }

  const auction = dto.auction ?? {}
  const route = dto.consolidatedRoute ?? {}

  return {
    auction: {
      id: auction.id ?? null,
      routeId: auction.routeId ?? null,
      openedAt: auction.openedAt ?? null,
      expiresAt: auction.expiresAt ?? null,
      automaticAward: Boolean(auction.automaticAward),
      status: auction.status ?? null,
    },
    consolidatedRoute: {
      id: route.id ?? null,
      status: route.status ?? null,
      totalDistanceKm: route.totalDistanceKm ?? 0,
      estimatedTimeHours: route.estimatedTimeHours ?? 0,
      totalWeightKg: route.totalWeightKg ?? 0,
      totalVolumeM3: route.totalVolumeM3 ?? 0,
      consolidatedCeiling: route.consolidatedCeiling ?? 0,
      estimatedAnttFloor: route.estimatedAnttFloor ?? 0,
    },
    updatedSegments: dto.updatedSegments ?? 0,
  }
}

/**
 * Creates a consolidated route and opens its auction.
 * @param {{ segmentIds: string[], expiresAt?: string, automaticAward?: boolean }} params
 */
export async function createAuction(
  { segmentIds, expiresAt, automaticAward } = {},
  options
) {
  const payload = await apiClient.post(
    BASE,
    {
      segmentIds: segmentIds ?? [],
      expiresAt: toIso(expiresAt),
      automaticAward: Boolean(automaticAward),
    },
    options
  )
  return toCreateAuctionResult(payload)
}

export const auctionService = {
  createAuction,
  toCreateAuctionResult,
}
