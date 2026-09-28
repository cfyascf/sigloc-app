/**
 * Freight offer service — boundary between the UI and the carrier-facing
 * opportunity board (Mural de Fretes) endpoints.
 *
 * Maps `GET /api/freight-offers` (`PagedFreightOffersDto`) and
 * `GET /api/freight-offers/{offerId}` (`FreightOfferDetailDto`) into plain,
 * UI-friendly shapes consumed by `FreightsOffersOverview`.
 *
 * All calls are authenticated (bearer token attached by the api-client) and
 * require carrier access on the backend.
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/freight-offers"

/** Normalizes a `FreightOfferListItemDto` into a plain object for the board cards. */
export function toFreightOffer(dto) {
  if (!dto) {
    return null
  }

  return {
    id: dto.id ?? null,
    contractor: dto.contractor ?? "",
    routeLabel: dto.routeLabel ?? "",
    targetValue: dto.targetValue ?? 0,
    totalBids: dto.totalBids ?? 0,
    pickupLabel: dto.pickupLabel ?? "—",
    etaLabel: dto.etaLabel ?? "—",
    totalWeight: dto.totalWeight ?? "",
    totalVolume: dto.totalVolume ?? "",
    requirements: dto.requirements ?? [],
    isExpiringSoon: Boolean(dto.isExpiringSoon),
    hoursLeft: dto.hoursLeft ?? 0,
    segmentId: dto.segmentId ?? null,
    risk: dto.risk ?? "NORMAL",
    bidStatus: dto.bidStatus ?? null,
  }
}

/** Normalizes a `FreightOfferDetailDto` into a plain object. */
export function toFreightOfferDetail(dto) {
  if (!dto) {
    return null
  }

  return {
    id: dto.id ?? null,
    segmentId: dto.segmentId ?? null,
    contractor: dto.contractor ?? "",
    routeLabel: dto.routeLabel ?? "",
    stops: dto.stops ?? [],
    targetValue: dto.targetValue ?? 0,
    totalBids: dto.totalBids ?? 0,
    requirements: dto.requirements ?? [],
    details: dto.details ?? "",
  }
}

function buildQuery({ search, risk, status, page, pageSize } = {}) {
  const params = new URLSearchParams()
  if (search) params.set("search", search)
  if (risk) params.set("risk", risk)
  if (status) params.set("status", status)
  if (page) params.set("page", String(page))
  if (pageSize) params.set("pageSize", String(pageSize))

  const query = params.toString()
  return query ? `?${query}` : ""
}

/**
 * Lists freight opportunities available to the current carrier, ordered by expiry.
 * @returns {Promise<{ items, page, pageSize, total }>}
 */
export async function listFreightOffers(query, options) {
  const payload = await apiClient.get(`${BASE}${buildQuery(query)}`, options)

  return {
    items: (payload?.items ?? []).map(toFreightOffer),
    page: payload?.page ?? 1,
    pageSize: payload?.pageSize ?? 20,
    total: payload?.total ?? 0,
  }
}

/** Fetches full detail for a single freight offer. */
export async function getFreightOfferById(offerId, options) {
  const payload = await apiClient.get(`${BASE}/${offerId}`, options)
  return toFreightOfferDetail(payload)
}

export const freightOfferService = {
  listFreightOffers,
  getFreightOfferById,
  toFreightOffer,
  toFreightOfferDetail,
}
