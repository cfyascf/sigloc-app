/**
 * Partner service — the single boundary between the UI and the Sigloc partner
 * network API. Maps the role-aware `GET /api/partnerships` endpoint into a plain,
 * UI-friendly shape: contractors receive the carrier network, carriers receive
 * the contractor network.
 *
 * All calls are authenticated (bearer token attached by the api-client); the
 * backend derives the caller's role from the JWT.
 */

import { apiClient } from "@/lib/api-client"

const BASE = "/api/partnerships"

/**
 * Backend partnership status (`ATIVA` | `PENDENTE` | `RECUSADA`) mapped to the
 * PT-BR labels the UI already styles in `PartnerNetwork`.
 */
const STATUS_LABELS = {
  ATIVA: "Ativo",
  PENDENTE: "Pendente de Aceite",
  RECUSADA: "Encerrado",
}

export function partnershipStatusLabel(status) {
  if (typeof status !== "string") {
    return "Encerrado"
  }

  return STATUS_LABELS[status.toUpperCase()] ?? "Encerrado"
}

/** First letter of the carrier trade name, used for the card avatar. */
function logoLetterFrom(name) {
  const trimmed = (name ?? "").trim()
  return trimmed ? trimmed[0].toUpperCase() : "?"
}

/**
 * Normalizes a single carrier-network `CarrierNetworkPartnerDto` (the contractor
 * side, from the carrier's point of view) into a plain object for the UI.
 *
 * `ofertasAbertas` counts the contractor's auctions open to bids now; `corredoresAtivos`
 * counts the distinct lanes the carrier runs for them on active trips.
 */
function toContractorPartner(dto) {
  const contractor = dto?.contratante ?? {}
  const name = contractor.nomeFantasia ?? "(contratante não encontrado)"

  return {
    id: dto?.conexaoId ?? null,
    name,
    logoLetter: logoLetterFrom(name),
    status: partnershipStatusLabel(dto?.statusParceria),
    cnpj: contractor.cnpj ?? "",
    lastInteraction: dto?.ultimaInteracao ?? null,
    openOffers: dto?.ofertasAbertas ?? 0,
    currentLanes: dto?.corredoresAtivos ?? 0,
  }
}

/** Normalizes a single `PartnerDto` into a plain object for the UI. */
function toPartner(dto) {
  const carrier = dto?.transportadora ?? {}
  const metrics = dto?.metricasOperacionais ?? {}
  const name = carrier.nomeFantasia ?? "(transportadora não encontrada)"

  return {
    id: dto?.conexaoId ?? null,
    name,
    logoLetter: logoLetterFrom(name),
    status: partnershipStatusLabel(dto?.statusParceria),
    cnpj: carrier.cnpj ?? "",
    averageRating: carrier.notaMedia ?? null,
    hasActiveInsurancePolicy: Boolean(carrier.apoliceSeguroAtiva),
    freeVehicles: metrics.veiculosLivres ?? 0,
    activeTripsWithUs: metrics.viagensAtivasConosco ?? 0,
    lastInteraction: metrics.ultimaInteracao ?? null,
  }
}

/**
 * Fetches the authenticated contractor's partner network (connected carriers).
 * @returns {Promise<{ totalActive, totalPending, partners: object[] }>}
 */
export async function getPartnerNetwork(options) {
  const payload = await apiClient.get(BASE, options)

  return {
    totalActive: payload?.totalAtivos ?? 0,
    totalPending: payload?.totalPendentes ?? 0,
    partners: (payload?.parceiros ?? []).map(toPartner),
  }
}

/**
 * Fetches the authenticated carrier's partner network (connected contractors).
 * Hits the same role-aware endpoint as {@link getPartnerNetwork}; the backend
 * returns the contractor-side payload for carriers, so only the mapper differs.
 * @returns {Promise<{ totalActive, totalPending, partners: object[] }>}
 */
export async function getCarrierNetwork(options) {
  const payload = await apiClient.get(BASE, options)

  return {
    totalActive: payload?.totalAtivos ?? 0,
    totalPending: payload?.totalPendentes ?? 0,
    partners: (payload?.parceiros ?? []).map(toContractorPartner),
  }
}

/**
 * Desfaz uma parceria (soft-remove). A conexão passa a inativa/encerrada e
 * continua visível no histórico; pode ser acionada por qualquer lado.
 * @param {string} connectionId id da conexão (campo `id` do parceiro na UI).
 */
export async function removePartnership(connectionId, options) {
  await apiClient.delete(`${BASE}/${connectionId}`, options)
}

export const partnerService = {
  getPartnerNetwork,
  getCarrierNetwork,
  partnershipStatusLabel,
  removePartnership,
}
