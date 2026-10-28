import { apiClient, ApiError } from "@/lib/api-client"
import { TRIP_OCCURRENCE_TYPES } from "@/constants/trip-occurrences"
import { listTripsAt, toProof, toStopTimeline, toTripDetails } from "@/services/trip-service"

const BASE = "/api/transportador/viagens"
const tripPath = (tripId) => `${BASE}/${encodeURIComponent(tripId)}`
const numberOrNull = (value) => typeof value === "number" && Number.isFinite(value) ? value : null
const textOrNull = (value) => typeof value === "string" && value.trim() ? value.trim() : null

// Links are only offered for values that cannot break out of the URI scheme.
const EMAIL_PATTERN = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/

export function toShipperContact(dto) {
  if (!dto) return null
  const phone = textOrNull(dto.telefone)
  const email = textOrNull(dto.email)
  const digits = phone?.replace(/\D/g, "") ?? ""
  return {
    companyName: textOrNull(dto.razaoSocial),
    tradeName: textOrNull(dto.nomeFantasia),
    contactName: textOrNull(dto.nomeContato),
    phone,
    email,
    phoneHref: digits.length >= 10 && digits.length <= 13 ? `tel:${phone.startsWith("+") ? "+" : ""}${digits}` : null,
    emailHref: email && EMAIL_PATTERN.test(email) ? `mailto:${email}` : null,
  }
}

export function toCargoSpecs(dto) {
  const cargo = dto ?? {}
  return {
    environment: textOrNull(cargo.ambiente),
    refrigerated: cargo.refrigerada === true,
    tempMin: numberOrNull(cargo.temperaturaMinima),
    tempMax: numberOrNull(cargo.temperaturaMaxima),
    palletized: cargo.paletizada === true,
    packagings: (cargo.embalagens ?? []).filter((item) => typeof item === "string" && item.trim()),
    dangerous: cargo.perigosa === true,
    fragile: cargo.fragil === true,
    bodywork: textOrNull(cargo.carroceria),
    totalWeightKg: numberOrNull(cargo.pesoTotalKg),
    totalVolumeM3: numberOrNull(cargo.volumeTotalM3),
    products: (cargo.produtos ?? []).map((product) => ({ name: product.nome ?? "Produto", quantity: numberOrNull(product.quantidade) })),
  }
}

export function toCurrentAction(dto) {
  if (!dto?.paradaId) return null
  return { stopId: dto.paradaId, type: dto.tipo, requiresProof: dto.exigeComprovante === true, deadline: dto.prazo ?? null }
}

export function toOccurrence(dto) {
  return {
    id: dto.id,
    type: dto.tipo,
    label: dto.rotulo ?? TRIP_OCCURRENCE_TYPES.find((type) => type.value === dto.tipo)?.label ?? dto.tipo,
    severity: dto.severidade,
    note: textOrNull(dto.observacao),
    reportedAt: dto.reportadaEm,
    stopId: dto.paradaId ?? null,
    resolvedAt: dto.encerradaEm ?? null,
    open: dto.aberta ?? !dto.encerradaEm,
  }
}

/** Shared trip detail (same shape the shipper screen uses) plus the carrier's operational context. */
export function toCarrierTripDetails(dto) {
  if (!dto?.viagem) {
    throw new ApiError("Resposta de viagem inválida.", { code: "INVALID_RESPONSE" })
  }
  return {
    ...toTripDetails(dto.viagem),
    stops: toStopTimeline(dto.viagem.paradas),
    shipper: toShipperContact(dto.embarcador),
    cargo: toCargoSpecs(dto.especificacaoCarga),
    currentAction: toCurrentAction(dto.acaoAtual),
    occurrences: (dto.ocorrencias ?? []).map(toOccurrence),
    proofs: (dto.comprovantes ?? []).map(toProof),
  }
}

export function listCarrierTrips(filters, options) {
  return listTripsAt(BASE, filters, options)
}

/** Delivered and cancelled trips of the carrier (status/risk filters do not apply). */
export function listCarrierTripHistory(filters, options) {
  return listTripsAt(BASE, filters, options, "historico")
}

/** `refresh: true` is the driver's "Transmitir Posição GPS" ping; a normal load returns the stored snapshot. */
export async function getCarrierTripDetails(tripId, { refresh = false, ...options } = {}) {
  const query = refresh ? "?refresh=true" : ""
  return toCarrierTripDetails(await apiClient.get(`${tripPath(tripId)}/detalhes${query}`, options))
}

/** Confirms the next pending stop. The proof file is required when `acaoAtual.exigeComprovante` is true. */
export async function completeTripStop(tripId, stopId, { file, note } = {}) {
  const form = new FormData()
  if (file) form.append("comprovante", file)
  if (note?.trim()) form.append("observacao", note.trim())
  return toCarrierTripDetails(await apiClient.post(`${tripPath(tripId)}/paradas/${encodeURIComponent(stopId)}/conclusao`, form))
}

export async function reportTripOccurrence(tripId, { type, note } = {}) {
  const body = { tipo: type }
  if (note?.trim()) body.observacao = note.trim()
  return toCarrierTripDetails(await apiClient.post(`${tripPath(tripId)}/ocorrencias`, body))
}

export function downloadTripProof(tripId, proofId) {
  return apiClient.get(`${tripPath(tripId)}/comprovantes/${encodeURIComponent(proofId)}`, { responseType: "blob" })
}

export const carrierTripService = {
  list: listCarrierTrips,
  listHistory: listCarrierTripHistory,
  getDetails: getCarrierTripDetails,
  completeStop: completeTripStop,
  reportOccurrence: reportTripOccurrence,
  downloadProof: downloadTripProof,
}
