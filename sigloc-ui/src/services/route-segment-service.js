/**
 * Route segment service — the single boundary between the UI and the Sigloc
 * route-segment API. Each function maps 1:1 to a backend endpoint and speaks the
 * exact API contract (`RouteSegmentRequestDto` / `RouteSegmentResponseDto` /
 * `PagedRouteSegmentsDto`).
 *
 * All calls are authenticated (bearer token attached by the api-client) and
 * require contractor/shipper access on the backend. Distance, travel time,
 * physical totals and the consolidated vehicle requirement are computed
 * server-side and returned as read-only fields.
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/route-segments"

const toNumber = (value) =>
  value === "" || value === null || value === undefined ? null : Number(value)

/**
 * Converts a `datetime-local` string (or any date-ish value) into an ISO-8601
 * string the backend accepts. Returns `null` when empty so validation errors
 * surface from the API rather than sending an invalid date.
 */
function toIso(value) {
  if (!value) {
    return null
  }

  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

/**
 * Converts an ISO timestamp into the `YYYY-MM-DDTHH:mm` shape a
 * `datetime-local` input expects (local time). Returns "" when absent.
 */
export function toDateTimeLocal(value) {
  if (!value) {
    return ""
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  const pad = (n) => String(n).padStart(2, "0")
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}`
  )
}

/**
 * Builds a `RouteSegmentRequestDto` payload from the form model. Only editable
 * fields are sent; computed values are never submitted.
 */
export function toRequestDto(form) {
  return {
    origin: form.origin?.trim() || null,
    destination: form.destination?.trim() || null,
    budgetCeiling: toNumber(form.budgetCeiling),
    estimatedTollCost: toNumber(form.estimatedTollCost),
    pickupDeadline: toIso(form.pickupDeadline),
    deliveryDeadline: toIso(form.deliveryDeadline),
    items: (form.items ?? [])
      .filter((item) => item && item.productId)
      .map((item) => ({
        productId: item.productId,
        quantity: toNumber(item.quantity),
      })),
  }
}

function toItem(dto) {
  return {
    productId: dto.productId,
    sku: dto.sku ?? "",
    name: dto.name ?? "",
    quantity: dto.quantity ?? 0,
    weightSubtotal: dto.weightSubtotal ?? 0,
    volumeSubtotal: dto.volumeSubtotal ?? 0,
  }
}

/**
 * Normalizes a `RouteSegmentResponseDto` into a plain object the UI can consume
 * directly, guarding against nulls.
 */
export function toRouteSegment(dto) {
  if (!dto) {
    return null
  }

  const totals = dto.calculatedTotals ?? {}
  const requirement = dto.consolidatedVehicleRequirement ?? {}

  return {
    id: dto.id,
    contractorId: dto.contractorId ?? null,
    routeId: dto.routeId ?? null,
    origin: dto.origin ?? "",
    destination: dto.destination ?? "",
    distanceKm: dto.distanceKm ?? 0,
    estimatedTimeHours: dto.estimatedTimeHours ?? 0,
    budgetCeiling: dto.budgetCeiling ?? null,
    estimatedTollCost: dto.estimatedTollCost ?? 0,
    pickupDeadline: dto.pickupDeadline ?? null,
    deliveryDeadline: dto.deliveryDeadline ?? null,
    status: dto.status ?? null,
    items: (dto.items ?? []).map(toItem),
    calculatedTotals: {
      totalWeightKg: totals.totalWeightKg ?? 0,
      totalVolumeM3: totals.totalVolumeM3 ?? 0,
    },
    consolidatedVehicleRequirement: {
      baseBodyworkType: requirement.baseBodyworkType ?? "",
      minRefrigerationLevel: requirement.minRefrigerationLevel ?? "",
      requiresMopp: Boolean(requirement.requiresMopp),
      requiresCargoFixing: Boolean(requirement.requiresCargoFixing),
    },
    createdAt: dto.createdAt ?? null,
  }
}

function buildQuery({ origin, destination, status, page, pageSize } = {}) {
  const params = new URLSearchParams()
  if (origin) params.set("origin", origin)
  if (destination) params.set("destination", destination)
  if (status) params.set("status", status)
  if (page) params.set("page", String(page))
  if (pageSize) params.set("pageSize", String(pageSize))

  const query = params.toString()
  return query ? `?${query}` : ""
}

/**
 * Lists route segments for the current contractor.
 * @returns {Promise<{ items, currentPage, pageSize, totalItems, totalPages }>}
 */
export async function listRouteSegments(query, options) {
  const payload = await apiClient.get(`${BASE}${buildQuery(query)}`, options)

  return {
    items: (payload?.items ?? []).map(toRouteSegment),
    currentPage: payload?.currentPage ?? 1,
    pageSize: payload?.pageSize ?? 20,
    totalItems: payload?.totalItems ?? 0,
    totalPages: payload?.totalPages ?? 0,
  }
}

/** Fetches a single route segment with full detail. */
export async function getRouteSegment(id, options) {
  const payload = await apiClient.get(`${BASE}/${id}`, options)
  return toRouteSegment(payload)
}

/** Creates a route segment from a form model. */
export async function createRouteSegment(form, options) {
  const payload = await apiClient.post(BASE, toRequestDto(form), options)
  return toRouteSegment(payload)
}

/** Updates an existing route segment from a form model. */
export async function updateRouteSegment(id, form, options) {
  const payload = await apiClient.put(`${BASE}/${id}`, toRequestDto(form), options)
  return toRouteSegment(payload)
}

/** Deletes a route segment. Resolves to void on success (204). */
export async function deleteRouteSegment(id, options) {
  await apiClient.delete(`${BASE}/${id}`, options)
}

export const routeSegmentService = {
  listRouteSegments,
  getRouteSegment,
  createRouteSegment,
  updateRouteSegment,
  deleteRouteSegment,
  toRequestDto,
  toRouteSegment,
  toDateTimeLocal,
}
