/**
 * Vehicle service — the single boundary between the UI and the Sigloc fleet
 * API. Each function maps 1:1 to a backend endpoint and speaks the exact API
 * contract (`VehicleResponseDto` / `UpdateVehicleDto`).
 *
 * All calls are authenticated (bearer token attached by the api-client) and
 * require carrier access on the backend.
 *
 * The API serializes enums as integers (no `JsonStringEnumConverter` is
 * registered), so this module converts them to/from the PT-BR labels the UI
 * uses via `constants/vehicles`.
 */

import { apiClient, ApiError } from "@/lib/api-client"
import {
  bodyTypeLabel,
  bodyTypeValue,
  statusLabel,
  statusValue,
} from "@/constants/vehicles"

const BASE = "/api/vehicles"

/**
 * Normalizes a `VehicleResponseDto` into the plain object the FleetManagement
 * page consumes. Enum ints become PT-BR labels; capacity/location fields are
 * renamed to the UI vocabulary. The enum-backed and UI-only fields the update
 * form doesn't expose (refrigerationLevel, hasMopp, hasCargoSecuring) are
 * preserved so they can be re-sent on update without being wiped.
 */
export function toVehicle(dto) {
  if (!dto) {
    return null
  }

  return {
    id: dto.id,
    transportadoraId: dto.transportadoraId,
    plate: dto.plate ?? "",
    model: dto.model ?? "",
    driver: dto.driver ?? "",
    driverPhone: dto.driverPhone ?? "",
    traccarDeviceId: dto.traccarDeviceId ?? "",
    location: dto.currentLocation ?? "",
    bodyType: bodyTypeLabel(dto.bodyType),
    status: statusLabel(dto.status),
    // Committed to a trip awaiting pickup or in transit; display-only, never sent on update.
    onActiveTrip: Boolean(dto.onActiveTrip),
    weightKg: dto.capacityWeight ?? 0,
    volumeM3: dto.capacityVolume ?? 0,
    // Preserved for round-tripping through update (no UI on this screen).
    axleCount: dto.axleCount ?? 0,
    refrigerationLevel: dto.refrigerationLevel ?? 0,
    hasMopp: Boolean(dto.hasMopp),
    hasCargoSecuring: Boolean(dto.hasCargoSecuring),
    ...documentsToForm(dto.documents),
    // Missing ("WARNING") or invalid ("BLOCKING") documents, as the API worked them out.
    compliance: dto.compliance ?? [],
  }
}

/**
 * Document fields of the vehicle form: enums as numbers ("" when unset), dates as "yyyy-mm-dd"
 * ("" when unset), EAR as true/false/null (unknown on vehicles saved before it was asked).
 */
export function documentsToForm(documents) {
  const d = documents ?? {}
  return {
    composition: d.composition ?? "",
    rntrcSource: d.rntrcSource ?? "",
    rntrcNumber: d.rntrcNumber ?? "",
    rntrcCategory: d.rntrcCategory ?? "",
    rntrcHolderName: d.rntrcHolderName ?? "",
    rntrcHolderDocument: d.rntrcHolderDocument ?? "",
    rntrcValidUntil: d.rntrcValidUntil ?? "",
    driverCnhCategory: d.driverCnhCategory ?? "",
    driverHasEar: d.driverHasEar ?? null,
    driverToxicologicalExamValidUntil: d.driverToxicologicalExamValidUntil ?? "",
    moppValidUntil: d.moppValidUntil ?? "",
    civCippValidUntil: d.civCippValidUntil ?? "",
  }
}

/** `VehicleDocumentsDto` from the form; blank fields go as null (on edit, null keeps the stored value). */
export function toDocumentsDto(form) {
  const text = (value) => (typeof value === "string" && value.trim() ? value.trim() : null)
  const number = (value) => (value === "" || value === null || value === undefined ? null : Number(value))
  return {
    composition: number(form.composition),
    rntrcSource: number(form.rntrcSource),
    rntrcNumber: text(form.rntrcNumber),
    rntrcCategory: number(form.rntrcCategory),
    rntrcHolderName: text(form.rntrcHolderName),
    rntrcHolderDocument: text(form.rntrcHolderDocument)?.replace(/\D/g, "") ?? null,
    rntrcValidUntil: text(form.rntrcValidUntil),
    driverCnhCategory: number(form.driverCnhCategory),
    driverHasEar: typeof form.driverHasEar === "boolean" ? form.driverHasEar : null,
    driverToxicologicalExamValidUntil: text(form.driverToxicologicalExamValidUntil),
    moppValidUntil: form.hasMopp ? text(form.moppValidUntil) : null,
    civCippValidUntil: text(form.civCippValidUntil),
  }
}

/**
 * Builds an `UpdateVehicleDto` from the UI vehicle model. `plate` is
 * intentionally omitted — the backend does not allow changing it on update.
 * Enum labels are converted back to their integer wire values.
 */
export function toUpdateDto(vehicle) {
  const toNumber = (value) =>
    value === "" || value === null || value === undefined ? null : Number(value)

  return {
    model: vehicle.model?.trim() || null,
    axleCount: toNumber(vehicle.axleCount),
    capacityWeight: toNumber(vehicle.weightKg),
    capacityVolume: toNumber(vehicle.volumeM3),
    bodyType: bodyTypeValue(vehicle.bodyType),
    refrigerationLevel: vehicle.refrigerationLevel ?? null,
    hasMopp: Boolean(vehicle.hasMopp),
    hasCargoSecuring: Boolean(vehicle.hasCargoSecuring),
    driver: vehicle.driver?.trim() || null,
    ...toTrackingDto(vehicle),
    currentLocation: vehicle.location?.trim() || null,
    status: statusValue(vehicle.status),
    documents: toDocumentsDto(vehicle),
  }
}

function toTrackingDto(vehicle) {
  const rawDeviceId = String(vehicle.traccarDeviceId ?? "").trim()
  const traccarDeviceId = rawDeviceId === "" ? null : Number(rawDeviceId)
  const driverPhone = vehicle.driverPhone == null ? null : vehicle.driverPhone.trim()
  if (traccarDeviceId !== null && (!Number.isSafeInteger(traccarDeviceId) || traccarDeviceId <= 0)) {
    throw new ApiError("O ID Traccar deve ser um número inteiro positivo válido.", { code: "VALIDATION_ERROR" })
  }
  if (driverPhone && driverPhone.length > 30) {
    throw new ApiError("O telefone deve ter no máximo 30 caracteres.", { code: "VALIDATION_ERROR" })
  }
  return { driverPhone, traccarDeviceId }
}

export function toCreateDto(vehicle) {
  const fields = toUpdateDto(vehicle)
  delete fields.status
  return {
    ...fields,
    driverPhone: fields.driverPhone || null,
    // The create endpoint rejects null for these non-nullable strings; blank is accepted.
    driver: fields.driver ?? "",
    currentLocation: fields.currentLocation ?? "",
    plate: vehicle.plate?.replace(/[\s-]/g, "").toUpperCase(),
    refrigerationLevel: vehicle.refrigerationLevel ?? 0,
  }
}

export async function createVehicle(vehicle, options) {
  return toVehicle(await apiClient.post(BASE, toCreateDto(vehicle), options))
}

/** Lists all vehicles for the current carrier. */
export async function listVehicles(options) {
  const payload = await apiClient.get(BASE, options)
  return (Array.isArray(payload) ? payload : []).map(toVehicle)
}

/** Fetches a single vehicle by id. */
export async function getVehicle(id, options) {
  const payload = await apiClient.get(`${BASE}/${id}`, options)
  return toVehicle(payload)
}

/**
 * Updates an existing vehicle from the UI model. Resolves to void on success
 * (the endpoint returns 204 No Content).
 */
export async function updateVehicle(id, vehicle, options) {
  await apiClient.put(`${BASE}/${id}`, toUpdateDto(vehicle), options)
}

/** Deletes a vehicle. Resolves to void on success (204). */
export async function deleteVehicle(id, options) {
  await apiClient.delete(`${BASE}/${id}`, options)
}

export const vehicleService = {
  createVehicle,
  listVehicles,
  getVehicle,
  updateVehicle,
  deleteVehicle,
  toVehicle,
  toUpdateDto,
}
