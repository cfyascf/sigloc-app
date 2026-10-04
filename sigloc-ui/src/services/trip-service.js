import { apiClient, ApiError } from "@/lib/api-client"

const BASE = "/api/viagens"
const numberOrNull = (value) => typeof value === "number" && Number.isFinite(value) ? value : null

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
  return {
    ...toActiveTrip(dto),
    carrier: (typeof dto.motorista === "object" ? dto.motorista?.transportadora : null) ?? toActiveTrip(dto).carrier,
    plate: (typeof dto.motorista === "object" ? dto.motorista?.placa : null) ?? toActiveTrip(dto).plate,
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
      })() : ({
        id: action.id, type: action.tipo, product: action.produto, deadline: action.prazo,
        completed: Boolean(action.concluidaEm), completedAt: action.concluidaEm, source: action.fonteConclusao,
      })),
    })),
    events: (dto.logEventos ?? dto.eventos ?? []).map((event) => ({
      id: event.id ?? `${event.ocorridoEm ?? event.hora}-${event.descricao}`,
      timestamp: event.ocorridoEm,
      message: event.descricao,
    })),
  }
}

export async function listActiveTrips({ search = "", status = "", risk = "", page = 1, pageSize = 20 } = {}, options) {
  const query = new URLSearchParams({
    page: String(Math.max(1, Math.trunc(page) || 1)),
    pageSize: String(Math.max(1, Math.min(100, Math.trunc(pageSize) || 20))),
  })
  if (search.trim()) query.set("search", search.trim())
  if (status) query.set("status", status)
  if (risk) query.set("risk", risk)
  const payload = await apiClient.get(`${BASE}/ativas?${query}`, options)
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
