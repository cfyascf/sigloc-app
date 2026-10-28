export const UNKNOWN = "Não informado"

export function formatTripDate(value) {
  if (!value) return UNKNOWN
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? UNKNOWN
    : new Intl.DateTimeFormat("pt-BR", {
        dateStyle: "short",
        timeStyle: "short",
      }).format(date)
}

export function formatTripNumber(value, unit = "") {
  return typeof value === "number" && Number.isFinite(value)
    ? `${new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 }).format(value)}${unit}`
    : UNKNOWN
}

const labels = {
  AGUARDANDO_COLETA: "Aguardando coleta",
  EM_TRANSITO: "Em trânsito",
  EM_CURSO: "Em curso",
  ATRASADO: "Atrasado",
  ENTREGUE: "Entregue",
  CONCLUIDO: "Concluído",
  CANCELADO: "Cancelado",
  CANCELADA: "Cancelada",
  CONCLUIDA: "Concluída",
  SEM_MONITORAMENTO: "Sem monitoramento",
  NAO_MONITORADO: "Sem monitoramento",
  NORMAL: "Normal",
  WARNING: "Atenção",
  ATENCAO: "Atenção",
  CRITIC: "Crítico",
  NO_PRAZO: "No prazo",
  PENDENTE: "Pendente",
  COLETA: "Coleta",
  ENTREGA: "Entrega",
  COLETA_E_ENTREGA: "Coleta e entrega",
}

/**
 * "traveled / total" for the progress card. Distance traveled comes from GPS refreshes, so a
 * trip delivered without them would stay "Não informado" forever; delivered means the whole route.
 */
export function formatRouteProgress(trip) {
  const traveled = trip?.lifecycle === "ENTREGUE" && trip?.traveled == null ? trip?.totalDistance : trip?.traveled
  return `${formatTripNumber(traveled, " km")} / ${formatTripNumber(trip?.totalDistance, " km")}`
}

export function tripStatusLabel(value) {
  return labels[value] ?? UNKNOWN
}

export function tripStatusStyle(value) {
  if (["ATRASADO", "CRITIC"].includes(value)) return "bg-rose-50 text-rose-700"
  if (["ENTREGUE", "CONCLUIDO", "CONCLUIDA", "NORMAL", "NO_PRAZO"].includes(value)) return "bg-emerald-50 text-emerald-700"
  if (["EM_TRANSITO", "EM_CURSO", "WARNING", "ATENCAO"].includes(value)) return "bg-amber-50 text-amber-700"
  return "bg-slate-100 text-slate-600"
}

const stopActionLabels = {
  COLETA: "Confirmar Coleta",
  ENTREGA: "Finalizar Entrega",
  COLETA_E_ENTREGA: "Confirmar Coleta e Entrega",
}

/** Button label for the carrier's current stop action (`acaoAtual.tipo`). */
export function stopActionLabel(type) {
  return stopActionLabels[type] ?? "Confirmar Parada"
}

export function formatFileSize(bytes) {
  if (typeof bytes !== "number" || !Number.isFinite(bytes)) return UNKNOWN
  // Below 1 KB the rounded value would read "0 KB" for a real file.
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${formatTripNumber(bytes / 1024)} KB`
  return `${formatTripNumber(bytes / (1024 * 1024))} MB`
}

/** KPI tint for an SLA status: rose when late, amber when at risk, none when on time. */
export function slaAlert(sla) {
  if (sla === "ATRASADO") return "rose"
  if (sla === "ATENCAO") return "amber"
  return undefined
}

/** "2h 05min", "04:59" within the last hour, "Atrasado há 12min" once overdue; `ms` comes from useCountdown. */
export function formatCountdown(ms) {
  if (typeof ms !== "number" || !Number.isFinite(ms)) return UNKNOWN
  const totalMinutes = Math.floor(Math.abs(ms) / 60000)
  const hours = Math.floor(totalMinutes / 60)
  const minutes = String(totalMinutes % 60).padStart(2, "0")
  if (ms < 0) return hours > 0 ? `Atrasado há ${hours}h ${minutes}min` : `Atrasado há ${totalMinutes}min`
  if (hours > 0) return `${hours}h ${minutes}min`
  const seconds = String(Math.floor(ms / 1000) % 60).padStart(2, "0")
  return `${minutes}:${seconds}`
}

/**
 * Criticality of a monitoring log event, derived from its `kind` (and the SLA description, since
 * the backend reuses the "SLA" kind for both escalations and recovery). Returns a label + badge
 * style used by the event log so occurrence severity shows up inline.
 */
export function eventSeverity(event) {
  const kind = event?.kind
  const message = (event?.message ?? "").toLowerCase()
  if (kind === "OCORRENCIA") {
    const critical = /crític|extrapolar|avaria|falta de doc|acidente|roubo|sinistro/.test(message)
    return critical
      ? { level: "CRITIC", label: "Crítico", style: "bg-rose-100 text-rose-700" }
      : { level: "WARNING", label: "Atenção", style: "bg-amber-100 text-amber-700" }
  }
  if (kind === "SLA") {
    if (/crític|excede|atras/.test(message)) return { level: "CRITIC", label: "Crítico", style: "bg-rose-100 text-rose-700" }
    if (/atenç|requer/.test(message)) return { level: "WARNING", label: "Atenção", style: "bg-amber-100 text-amber-700" }
    return { level: "NORMAL", label: "SLA", style: "bg-emerald-100 text-emerald-700" }
  }
  if (kind === "OCORRENCIA_ENCERRADA") return { level: "RESOLVED", label: "Encerrada", style: "bg-emerald-100 text-emerald-700" }
  if (kind === "PARADA_CONFIRMADA") return { level: "INFO", label: "Parada", style: "bg-blue-100 text-blue-700" }
  if (kind === "PARADA_SUSPEITA") return { level: "WARNING", label: "Suspeita", style: "bg-amber-100 text-amber-700" }
  return { level: "INFO", label: "Info", style: "bg-slate-100 text-slate-600" }
}
