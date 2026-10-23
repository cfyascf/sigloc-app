/**
 * Profile service — boundary between the UI and the Sigloc profile API.
 * Maps the role-aware `GET/PUT /api/profile` endpoints into a plain, UI-friendly
 * shape. The backend derives the caller's company and role from the JWT.
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/profile"

/** Normalizes a `CompanyProfileDto` into a plain object for the UI. */
function toProfile(dto) {
  if (!dto) {
    return null
  }

  return {
    id: dto.id ?? null,
    role: dto.role ?? null,
    cnpj: dto.cnpj ?? "",
    companyName: dto.companyName ?? "",
    tradeName: dto.tradeName ?? "",
    averageRating: dto.averageRating ?? null,
    hasActiveInsurancePolicy: dto.hasActiveInsurancePolicy ?? null,
    onTimeDeliveryRate: dto.onTimeDeliveryRate ?? null,
    // Shipper-only "Contato Logístico" shown to carriers on the trips they operate.
    logisticsContactName: dto.logisticsContactName ?? "",
    logisticsContactPhone: dto.logisticsContactPhone ?? "",
    logisticsContactEmail: dto.logisticsContactEmail ?? "",
  }
}

const CONTACT_FIELDS = ["logisticsContactName", "logisticsContactPhone", "logisticsContactEmail"]

/** Fetches the authenticated user's company profile. */
export async function getProfile(options) {
  const payload = await apiClient.get(BASE, options)
  return toProfile(payload)
}

/**
 * Updates the editable company data. The company name is always required; logistics
 * contact fields are only sent when provided (omitted keeps the stored value, "" clears it).
 */
export async function updateProfile({ companyName, tradeName, ...contact }, options) {
  const body = { companyName, tradeName }
  for (const field of CONTACT_FIELDS) {
    if (contact[field] !== undefined) body[field] = contact[field].trim()
  }
  const payload = await apiClient.put(BASE, body, options)
  return toProfile(payload)
}

export const profileService = {
  getProfile,
  updateProfile,
}
