/**
 * Partner service — the single boundary between the UI and the Sigloc partner
 * network API. Maps `GET /api/partnerships` (`PartnerNetworkDto`) into a plain,
 * UI-friendly shape.
 *
 * All calls are authenticated (bearer token attached by the api-client) and
 * require contractor/shipper access on the backend.
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
 * Fetches the authenticated contractor's partner network.
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

export const partnerService = {
  getPartnerNetwork,
  partnershipStatusLabel,
}
