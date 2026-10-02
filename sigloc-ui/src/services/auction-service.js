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
 * Updates an auction's bid deadline. The deadline is only sent when provided.
 * @param {string} id
 * @param {{ expiresAt?: string|Date }} changes
 */
export async function updateAuction(id, { expiresAt } = {}, options) {
  const payload = {}
  if (expiresAt !== undefined) {
    payload.expiresAt = toIso(expiresAt)
  }

  const dto = await apiClient.put(`${BASE}/${id}`, payload, options)
  return dto
    ? {
        id: dto.id ?? id,
        expiresAt: dto.expiresAt ?? null,
      }
    : null
}

/** Deletes an auction by id. */
export async function deleteAuction(id, options) {
  await apiClient.delete(`${BASE}/${id}`, options)
  return { id }
}

/** Normalizes a `BidRankingDto` into a plain object for the bid-analysis screen. */
export function toBidRanking(dto) {
  if (!dto) {
    return null
  }

  const route = dto.route ?? {}
  const scenario = route.financialScenario ?? {}

  return {
    auctionId: dto.auctionId ?? null,
    status: dto.status ?? null,
    route: {
      routeId: route.routeId ?? null,
      itinerarySummary: route.itinerarySummary ?? "",
      itineraryWithStates: route.itineraryWithStates ?? "",
      segmentCount: route.segmentCount ?? 0,
      firstSegmentId: route.firstSegmentId ?? null,
      linkedSegmentIds: route.linkedSegmentIds ?? [],
      firstPickupDeadline: route.firstPickupDeadline ?? null,
      lastDeliveryDeadline: route.lastDeliveryDeadline ?? null,
      financialScenario: {
        consolidatedCeiling: scenario.consolidatedCeiling ?? 0,
        estimatedAnttFloor: scenario.estimatedAnttFloor ?? 0,
      },
    },
    bids: (dto.bids ?? []).map((bid) => {
      const carrier = bid.carrier ?? {}
      const vehicle = bid.vehicle ?? {}
      const financials = bid.financials ?? {}

      return {
        rank: bid.rank ?? 0,
        bidId: bid.bidId ?? null,
        status: bid.status ?? null,
        submittedAt: bid.submittedAt ?? null,
        carrier: {
          id: carrier.id ?? null,
          tradeName: carrier.tradeName ?? "",
          averageRating: carrier.averageRating ?? null,
          onTimeDeliveryRate: carrier.onTimeDeliveryRate ?? 0,
        },
        vehicle: {
          plate: vehicle.plate ?? "",
          bodyType: vehicle.bodyType ?? "",
          axleCount: vehicle.axleCount ?? 0,
          capacityWeightKg: vehicle.capacityWeightKg ?? 0,
          capacityVolumeM3: vehicle.capacityVolumeM3 ?? 0,
        },
        financials: {
          totalValue: financials.totalValue ?? 0,
          netFreightValue: financials.netFreightValue ?? 0,
          tollValue: financials.tollValue ?? 0,
          anttFreightFloor: financials.anttFreightFloor ?? null,
          savingsValue: financials.savingsValue ?? 0,
          savingsPercentage: financials.savingsPercentage ?? 0,
        },
      }
    }),
  }
}

/** Fetches the ranked list of bids for an auction (Motor de Ranking). */
export async function getBidRanking(auctionId, options) {
  const payload = await apiClient.get(`${BASE}/${auctionId}/bids`, options)
  return toBidRanking(payload)
}

/**
 * Awards the auction to the chosen bid (Adjudicação). Returns the created trip id.
 * @param {string} auctionId
 * @param {string} winningBidId
 */
export async function awardAuction(auctionId, winningBidId, options) {
  const dto = await apiClient.post(
    `${BASE}/${auctionId}/award`,
    { winningBidId },
    options
  )
  return dto
    ? {
        tripId: dto.tripId ?? null,
        auctionId: dto.auctionId ?? auctionId,
        winningBidId: dto.winningBidId ?? winningBidId,
        auctionStatus: dto.auctionStatus ?? null,
        routeStatus: dto.routeStatus ?? null,
      }
    : null
}

/** Normalizes a `CarrierBidAnalysisDto` into a plain object for the bid workspace. */
export function toCarrierAnalysis(dto) {
  if (!dto) {
    return null
  }

  const summary = dto.routeSummary ?? {}
  const sla = summary.sla ?? {}
  const competition = dto.competition ?? {}
  const physical = dto.physicalRequirements ?? {}

  return {
    auctionId: dto.auctionId ?? null,
    routeSummary: {
      referenceCode: summary.referenceCode ?? "",
      shortItinerary: summary.shortItinerary ?? "",
      sla: {
        firstPickup: sla.firstPickup ?? null,
        lastDelivery: sla.lastDelivery ?? null,
      },
    },
    competition: {
      activeBids: competition.activeBids ?? 0,
      bestLeaderOffer: competition.bestLeaderOffer ?? null,
      auctionCeiling: competition.auctionCeiling ?? 0,
    },
    physicalRequirements: {
      recommendedFleet: physical.recommendedFleet ?? "",
      consolidatedWeightKg: physical.consolidatedWeightKg ?? 0,
      volumeM3: physical.volumeM3 ?? 0,
      requiredTemperature: physical.requiredTemperature ?? "",
      handlingRestrictions: physical.handlingRestrictions ?? [],
    },
    carrierAvailableFleet: (dto.carrierAvailableFleet ?? []).map((v) => ({
      vehicleId: v.vehicleId ?? null,
      plate: v.plate ?? "",
      model: v.model ?? "",
      capacityWeightKg: v.capacityWeightKg ?? 0,
      capacityVolumeM3: v.capacityVolumeM3 ?? 0,
      specifications: v.specifications ?? [],
    })),
    travelPlan: (dto.travelPlan ?? []).map((stop) => ({
      order: stop.order ?? 0,
      city: stop.city ?? "",
      action: stop.action ?? "",
    })),
    myBid: dto.myBid
      ? {
          bidId: dto.myBid.bidId ?? null,
          vehicleId: dto.myBid.vehicleId ?? null,
          netFreightValue: dto.myBid.netFreightValue ?? 0,
          tollValue: dto.myBid.tollValue ?? 0,
          totalValue: dto.myBid.totalValue ?? 0,
          submittedAt: dto.myBid.submittedAt ?? null,
          status: dto.myBid.status ?? null,
        }
      : null,
  }
}

/** Fetches the carrier bid workspace (Workspace de Lance) for an auction. */
export async function getCarrierAnalysis(auctionId, options) {
  const payload = await apiClient.get(
    `${BASE}/${auctionId}/carrier-analysis`,
    options
  )
  return toCarrierAnalysis(payload)
}

/**
 * Submits a carrier bid. The backend runs the Constraint Engine and either persists
 * the bid (201) or rejects it (400) with an `ApiError` carrying the exact motive.
 * @param {string} auctionId
 * @param {{ valorOferecido: number, veiculoId: string }} bid
 */
export async function placeBid(auctionId, { valorOferecido, veiculoId } = {}, options) {
  const dto = await apiClient.post(
    `${BASE}/${auctionId}/bids`,
    { valorOferecido, veiculoId },
    options
  )
  return dto
    ? {
        bidId: dto.bidId ?? null,
        auctionId: dto.auctionId ?? auctionId,
        netFreightValue: dto.netFreightValue ?? 0,
        tollValue: dto.tollValue ?? 0,
        totalValue: dto.totalValue ?? 0,
        submittedAt: dto.submittedAt ?? null,
        status: dto.status ?? null,
      }
    : null
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
  getBidRanking,
  toBidRanking,
  awardAuction,
  getCarrierAnalysis,
  toCarrierAnalysis,
  placeBid,
}
