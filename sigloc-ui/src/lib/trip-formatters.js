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

export function tripStatusLabel(value) {
  return labels[value] ?? UNKNOWN
}

export function tripStatusStyle(value) {
  if (["ATRASADO", "CRITIC"].includes(value)) return "bg-rose-50 text-rose-700"
  if (["ENTREGUE", "CONCLUIDO", "CONCLUIDA", "NORMAL", "NO_PRAZO"].includes(value)) return "bg-emerald-50 text-emerald-700"
  if (["EM_TRANSITO", "EM_CURSO", "WARNING", "ATENCAO"].includes(value)) return "bg-amber-50 text-amber-700"
  return "bg-slate-100 text-slate-600"
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
