import { apiClient, ApiError } from "@/lib/api-client"

const BASE = "/api/viagens"
const numberOrNull = (value) => typeof value === "number" && Number.isFinite(value) ? value : null

const toStopAction = (action) => ({
  id: action.id, type: action.tipo, product: action.produto, deadline: action.prazo,
  completed: Boolean(action.concluidaEm), completedAt: action.concluidaEm, source: action.fonteConclusao,
})

/** One node per stop from `paradas`; unlike the city-grouped `linhaDoTempo`, it keeps the stop IDs actions need. */
export function toStopTimeline(paradas) {
  return [...(paradas ?? [])]
    .sort((a, b) => (a.ordem ?? 0) - (b.ordem ?? 0))
    .map((stop) => ({
      id: stop.id,
      city: [stop.cidade, stop.estado].filter(Boolean).join(", "),
      status: stop.status === "CONCLUIDA" ? "CONCLUIDO" : stop.status,
      completedAt: stop.concluidaEm,
      source: stop.fonteConclusao,
      actions: (stop.acoes ?? []).map(toStopAction),
    }))
}

export function toProof(dto) {
  return {
    id: dto.id,
    stopId: dto.paradaId,
    fileName: dto.nomeArquivo ?? "comprovante",
    contentType: dto.tipoConteudo,
    sizeBytes: numberOrNull(dto.tamanhoBytes),
    uploadedAt: dto.enviadoEm,
  }
}

export function toActiveTrip(dto) {
  const cache = dto.monitoramentoCache ?? {}
  const carrier = typeof dto.transportadora === "object" ? dto.transportadora?.nomeFantasia : dto.transportadora
  const plate = typeof dto.veiculo === "object" ? dto.veiculo?.placa : dto.placa
  return {
    id: dto.viagemId ?? dto.id,
    routeId: dto.rotaId,
    reference: dto.codigoReferencia ?? dto.referencia,
    status: dto.statusGeral ?? dto.status,
    lifecycle: dto.statusViagem ?? (["ENTREGUE", "CANCELADA", "CANCELADO"].includes(dto.statusGeral ?? dto.status) ? (dto.statusGeral ?? dto.status) : null),
    risk: dto.indicadorRisco ?? dto.risco,
    carrier,
    plate,
    shipperName: typeof dto.embarcador === "string" ? dto.embarcador : null,
    origin: dto.origem,
    destination: dto.destino,
    progress: numberOrNull(dto.progressoPercentual ?? cache.progressoPercentual),
    eta: dto.novoEta ?? cache.ultimoEtaCalculado,
    lastPing: dto.ultimaPosicaoEm ?? cache.ultimaAtualizacaoPing,
    lastCalculation: dto.ultimaAtualizacaoCalculada ?? dto.ultimaAtualizacao,
  }
}

export function toTripDetails(dto) {
  if (!dto) return null
  const health = dto.saudeOperacao ?? {}
  const context = dto.contextoGeografico ?? {}
  const driver = typeof dto.motorista === "object" ? dto.motorista : { nome: dto.motorista, telefone: dto.telefoneMotorista }
  const timeline = dto.linhaDoTempo ?? (dto.paradas ?? []).map((stop) => ({
    id: stop.id,
    cidade: `${stop.cidade}, ${stop.estado}`,
    tipoNode: "Parada intermediária",
    acoes: stop.acoes ?? [],
    status: stop.status === "CONCLUIDA" ? "CONCLUIDO" : stop.status,
    dataHoraRealizada: stop.concluidaEm,
    deadlineSla: (stop.acoes ?? []).find((action) => !action.concluidaEm)?.prazo,
  }))
  const base = toActiveTrip(dto)
  return {
    ...base,
    carrier: (typeof dto.motorista === "object" ? dto.motorista?.transportadora : null) ?? base.carrier,
    plate: (typeof dto.motorista === "object" ? dto.motorista?.placa : null) ?? base.plate,
    eta: health.novoEta ?? health.ultimoEtaCalculado ?? base.eta,
    traveled: numberOrNull(health.distanciaPercorridaKm ?? dto.distanciaPercorridaKm),
    totalDistance: numberOrNull(health.distanciaTotalKm ?? dto.distanciaTotalKm),
    remaining: numberOrNull(dto.distanciaRestanteKm),
    utilization: numberOrNull(health.utilizacaoCargaPercentual ?? dto.ocupacaoPercentual),
    sla: health.statusSla ?? dto.indicadorRisco ?? dto.risco,
    nextDeadline: dto.prazoProximaParada,
    cacheRenewed: health.isCacheRenovado ?? dto.isCacheRenovado === true,
    stale: dto.cacheDesatualizado === true,
    warning: dto.aviso,
    latitude: numberOrNull(context.ultimaCoordenada?.lat ?? dto.latitude),
    longitude: numberOrNull(context.ultimaCoordenada?.lng ?? dto.longitude),
    lastPing: context.ultimoPing ?? dto.ultimaPosicaoEm,
    startedAt: dto.inicio,
    endedAt: dto.fim,
    driver: { name: driver?.nome, phone: driver?.telefone },
    timeline: timeline.map((stop, index) => ({
      id: stop.id ?? `${stop.cidade}-${index}`,
      city: stop.cidade,
      state: "",
      status: stop.status,
      color: stop.cor ?? ({ CONCLUIDA: "VERDE", CONCLUIDO: "VERDE", EM_TRANSITO: "AMARELO", PENDENTE: "CINZA" }[stop.status]),
      actions: (stop.acoes ?? []).map((action) => typeof action === "string" ? (() => {
        const [type, product] = action.split(" · ", 2)
        return { id: action, type, product: product ?? null, deadline: stop.deadlineSla, completed: stop.status === "CONCLUIDO", completedAt: stop.dataHoraRealizada }
      })() : toStopAction(action)),
    })),
    proofs: (dto.comprovantes ?? []).map(toProof),
    events: (dto.logEventos ?? dto.eventos ?? []).map((event) => ({
      id: event.id ?? `${event.ocorridoEm ?? event.hora}-${event.descricao}`,
      timestamp: event.ocorridoEm,
      message: event.descricao,
      kind: event.tipo ?? null,
    })),
  }
}

export function listActiveTrips(filters, options) {
  return listTripsAt(BASE, filters, options)
}

/** Paged history list (finished trips: delivered/cancelled). Status/risk filters do not apply server-side. */
export function listTripHistory(filters, options) {
  return listTripsAt(BASE, filters, options, "historico")
}

/** Paged trip list; shippers and carriers share the query and response shape under different bases/endpoints. */
export async function listTripsAt(base, { search = "", status = "", risk = "", page = 1, pageSize = 20 } = {}, options, endpoint = "ativas") {
  const query = new URLSearchParams({
    page: String(Math.max(1, Math.trunc(page) || 1)),
    pageSize: String(Math.max(1, Math.min(100, Math.trunc(pageSize) || 20))),
  })
  if (search.trim()) query.set("search", search.trim())
  if (status) query.set("status", status)
  if (risk) query.set("risk", risk)
  const payload = await apiClient.get(`${base}/${endpoint}?${query}`, options)
  if (!payload || !Array.isArray(payload.viagens)) {
    throw new ApiError("Resposta de viagens inválida.", { code: "INVALID_RESPONSE" })
  }
  return {
    trips: payload.viagens.map(toActiveTrip),
    total: payload.totalItems,
    totalPages: payload.totalPages,
    page: payload.page,
    pageSize: payload.pageSize,
  }
}

export async function getTripDetails(tripId, { refresh = false, ...options } = {}) {
  // GPS is only fetched server-side when refresh=true (the "Locate driver" button).
  // A normal page load omits it and receives the last stored snapshot.
  const query = refresh ? "?refresh=true" : ""
  return toTripDetails(await apiClient.get(`${BASE}/${encodeURIComponent(tripId)}/detalhes${query}`, options))
}

/** Proof-of-delivery file the carrier uploaded; the endpoint needs the bearer token, so it comes back as a Blob. */
export function downloadTripProof(tripId, proofId) {
  return apiClient.get(`${BASE}/${encodeURIComponent(tripId)}/comprovantes/${encodeURIComponent(proofId)}`, { responseType: "blob" })
}
