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
  CRITIC: "Crítico",
  PENDENTE: "Pendente",
  COLETA: "Coleta",
  ENTREGA: "Entrega",
}

export function tripStatusLabel(value) {
  return labels[value] ?? UNKNOWN
}

export function tripStatusStyle(value) {
  if (["ATRASADO", "CRITIC"].includes(value)) return "bg-rose-50 text-rose-700"
  if (["ENTREGUE", "CONCLUIDO", "CONCLUIDA", "NORMAL"].includes(value)) return "bg-emerald-50 text-emerald-700"
  if (["EM_TRANSITO", "EM_CURSO"].includes(value)) return "bg-amber-50 text-amber-700"
  return "bg-slate-100 text-slate-600"
}
