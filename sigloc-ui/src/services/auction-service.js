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

/** Normalizes an `AuctionListItemDto` into a plain object for the listing panel. */
export function toAuctionListItem(dto) {
  if (!dto) {
    return null
  }

  const metrics = dto.bidMetrics ?? {}

  return {
    id: dto.id ?? null,
    routeId: dto.routeId ?? null,
    status: dto.status ?? null,
    itinerarySummary: dto.itinerarySummary ?? "",
    itineraryWithStates: dto.itineraryWithStates ?? "",
    linkedSegments: dto.linkedSegments ?? [],
    riskIndicator: dto.riskIndicator ?? "NORMAL",
    expiresAt: dto.expiresAt ?? null,
    bidMetrics: {
      bestBid: metrics.bestBid ?? null,
      totalBids: metrics.totalBids ?? 0,
    },
  }
}

/** Normalizes an `AuctionDetailDto` into a plain object for the route detail screen. */
export function toAuctionDetail(dto) {
  if (!dto) {
    return null
  }

  const route = dto.route ?? {}
  const scenario = route.financialScenario ?? {}
  const metrics = dto.bidMetrics ?? {}
  const bestBid = metrics.bestBid ?? null

  return {
    id: dto.id ?? null,
    status: dto.status ?? null,
    expiresAt: dto.expiresAt ?? null,
    route: {
      id: route.id ?? null,
      status: route.status ?? null,
      formattedName: route.formattedName ?? "",
      totalDistanceKm: route.totalDistanceKm ?? 0,
      totalWeightKg: route.totalWeightKg ?? 0,
      totalVolumeM3: route.totalVolumeM3 ?? 0,
      consolidatedVehicleRequirement: route.consolidatedVehicleRequirement ?? "",
      financialScenario: {
        consolidatedCeiling: scenario.consolidatedCeiling ?? 0,
        estimatedAnttFloor: scenario.estimatedAnttFloor ?? 0,
      },
    },
    bidMetrics: {
      totalBids: metrics.totalBids ?? 0,
      bestBid: bestBid
        ? {
            value: bestBid.value ?? 0,
            carrierName: bestBid.carrierName ?? "",
          }
        : null,
    },
    travelPlan: (dto.travelPlan ?? []).map((stop) => ({
      order: stop.order ?? 0,
      cityState: stop.cityState ?? "",
      actionType: stop.actionType ?? "",
      deadline: stop.deadline ?? null,
    })),
    segments: (dto.segments ?? []).map((segment) => ({
      id: segment.id ?? null,
      mainProduct: segment.mainProduct ?? "",
      origin: segment.origin ?? "",
      destination: segment.destination ?? "",
      financialCeiling: segment.financialCeiling ?? null,
    })),
  }
}

function buildQuery({ search, status, page, pageSize } = {}) {
  const params = new URLSearchParams()
  if (search) params.set("search", search)
  if (status) params.set("status", status)
  if (page) params.set("page", String(page))
  if (pageSize) params.set("pageSize", String(pageSize))

  const query = params.toString()
  return query ? `?${query}` : ""
}

/**
 * Lists auctions for the current contractor, ordered by expiry (soonest first).
 * @returns {Promise<{ items, currentPage, pageSize, totalItems, totalPages }>}
 */
export async function listAuctions(query, options) {
  const payload = await apiClient.get(`${BASE}${buildQuery(query)}`, options)

  return {
    items: (payload?.items ?? []).map(toAuctionListItem),
    currentPage: payload?.currentPage ?? 1,
    pageSize: payload?.pageSize ?? 20,
    totalItems: payload?.totalItems ?? 0,
    totalPages: payload?.totalPages ?? 0,
  }
}

/** Fetches a single auction with its deep route detail (Milking Run + financial scenario). */
export async function getAuctionById(id, options) {
  const payload = await apiClient.get(`${BASE}/${id}`, options)
  return toAuctionDetail(payload)
}

/**
 * Updates an auction's editable metadata (custom name and/or bid deadline). Only the
 * provided fields are sent.
 * @param {string} id
 * @param {{ name?: string, expiresAt?: string|Date }} changes
 */
export async function updateAuction(id, { name, expiresAt } = {}, options) {
  const payload = {}
  if (name !== undefined) {
    payload.name = name
  }
  if (expiresAt !== undefined) {
    payload.expiresAt = toIso(expiresAt)
  }

  const dto = await apiClient.put(`${BASE}/${id}`, payload, options)
  return dto
    ? {
        id: dto.id ?? id,
        name: dto.name ?? null,
        expiresAt: dto.expiresAt ?? null,
      }
    : null
}

/** Deletes an auction by id. */
export async function deleteAuction(id, options) {
  await apiClient.delete(`${BASE}/${id}`, options)
  return { id }
}

export const auctionService = {
  createAuction,
  toCreateAuctionResult,
  listAuctions,
  getAuctionById,
  updateAuction,
  deleteAuction,
  toAuctionListItem,
  toAuctionDetail,
}
