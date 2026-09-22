/**
 * Product service — the single boundary between the UI and the Sigloc product
 * API. Each function maps 1:1 to a backend endpoint and speaks the exact API
 * contract (`ProductRequestDto` / `ProductResponseDto` / `PagedProductsDto`).
 *
 * All calls are authenticated (bearer token attached by the api-client) and
 * require contractor/shipper access on the backend.
 */

import { apiClient } from "@/lib/api-client"
import {
  requiresPackaging,
  requiresTemperature,
} from "@/constants/products"

const BASE = "/api/products"

/**
 * Builds a `ProductRequestDto` payload from the form model, applying the same
 * conditional rules the backend enforces so we never send meaningless values:
 *   - Temperature is only sent when the environment is not "Seco".
 *   - Packaging type is only sent when the category is "Geral".
 */
export function toRequestDto(form) {
  const needsTemp = requiresTemperature(form.transportEnvironment)
  const needsPackaging = requiresPackaging(form.category)

  const toNumber = (value) =>
    value === "" || value === null || value === undefined
      ? null
      : Number(value)

  return {
    sku: form.sku?.trim() || null,
    name: form.name?.trim() || null,
    type: form.type?.trim() || null,
    category: form.category || null,
    transportEnvironment: form.transportEnvironment || null,
    tempMin: needsTemp ? toNumber(form.tempMin) : null,
    tempMax: needsTemp ? toNumber(form.tempMax) : null,
    packagingType: needsPackaging ? form.packagingType || null : null,
    dangerous: Boolean(form.dangerous),
    fragile: Boolean(form.fragile),
    defaultWeight: toNumber(form.defaultWeight),
    defaultVolume: toNumber(form.defaultVolume),
    handlingRestriction: form.handlingRestriction?.trim() || null,
  }
}

/**
 * Normalizes a `ProductResponseDto` into a plain object the UI can consume
 * directly. The shape already matches the API camelCase contract, so this is a
 * light pass-through that guards against nulls.
 */
function toProduct(dto) {
  if (!dto) {
    return null
  }

  return {
    id: dto.id,
    contractorId: dto.contractorId,
    sku: dto.sku,
    name: dto.name,
    type: dto.type ?? "",
    category: dto.category,
    transportEnvironment: dto.transportEnvironment,
    tempMin: dto.tempMin ?? null,
    tempMax: dto.tempMax ?? null,
    packagingType: dto.packagingType ?? "",
    dangerous: Boolean(dto.dangerous),
    fragile: Boolean(dto.fragile),
    defaultWeight: dto.defaultWeight,
    defaultVolume: dto.defaultVolume,
    handlingRestriction: dto.handlingRestriction ?? "",
    vehicleRequirement: dto.vehicleRequirement ?? null,
    createdAt: dto.createdAt ?? null,
    updatedAt: dto.updatedAt ?? null,
  }
}

function buildQuery({ search, category, page, pageSize } = {}) {
  const params = new URLSearchParams()
  if (search) params.set("search", search)
  if (category) params.set("category", category)
  if (page) params.set("page", String(page))
  if (pageSize) params.set("pageSize", String(pageSize))

  const query = params.toString()
  return query ? `?${query}` : ""
}

/**
 * Lists products for the current contractor.
 * @returns {Promise<{ items, currentPage, pageSize, totalItems, totalPages }>}
 */
export async function listProducts(query, options) {
  const payload = await apiClient.get(`${BASE}${buildQuery(query)}`, options)

  return {
    items: (payload?.items ?? []).map((item) => ({
      id: item.id,
      sku: item.sku,
      name: item.name,
      category: item.category,
      transportEnvironment: item.transportEnvironment,
      dangerous: Boolean(item.dangerous),
      fragile: Boolean(item.fragile),
      defaultWeight: item.defaultWeight,
      defaultVolume: item.defaultVolume,
    })),
    currentPage: payload?.currentPage ?? 1,
    pageSize: payload?.pageSize ?? 20,
    totalItems: payload?.totalItems ?? 0,
    totalPages: payload?.totalPages ?? 0,
  }
}

/** Fetches a single product with full detail (including vehicle requirement). */
export async function getProduct(id, options) {
  const payload = await apiClient.get(`${BASE}/${id}`, options)
  return toProduct(payload)
}

/** Creates a product from a form model. */
export async function createProduct(form, options) {
  const payload = await apiClient.post(BASE, toRequestDto(form), options)
  return toProduct(payload)
}

/** Updates an existing product from a form model. */
export async function updateProduct(id, form, options) {
  const payload = await apiClient.put(`${BASE}/${id}`, toRequestDto(form), options)
  return toProduct(payload)
}

/** Deletes a product. Resolves to void on success (204). */
export async function deleteProduct(id, options) {
  await apiClient.delete(`${BASE}/${id}`, options)
}

export const productService = {
  listProducts,
  getProduct,
  createProduct,
  updateProduct,
  deleteProduct,
  toRequestDto,
}
