import { MapPinned } from "lucide-react"

import { TripCard } from "@/components/trips/trip-card"
import { formatTripDate, tripStatusLabel } from "@/lib/trip-formatters"

const dotColor = (status) =>
  status === "CONCLUIDO" || status === "CONCLUIDA"
    ? "bg-emerald-500"
    : status === "EM_TRANSITO"
      ? "bg-amber-500 ring-4 ring-amber-50"
      : "bg-slate-300"

// For a stop, EM_TRANSITO marks the stop the truck is heading to, not a truck in motion
// (it is also set while the trip still awaits pickup).
const stopStatusLabel = (status) => (status === "EM_TRANSITO" ? "Próxima parada" : tripStatusLabel(status))

const actionLabel = (action) =>
  [action.type ? tripStatusLabel(action.type) : null, action.product].filter(Boolean).join(" · ")

function earliest(values) {
  const times = values.map((value) => new Date(value).getTime()).filter(Number.isFinite)
  return times.length ? new Date(Math.min(...times)) : null
}

/**
 * Stop history. `renderStopActions(stop)` lets a screen add controls to a stop
 * (the carrier's "Confirmar Coleta" / "Finalizar Entrega").
 */
export function TripStopTimeline({ stops, renderStopActions }) {
  return (
    <TripCard icon={MapPinned} iconClassName="text-blue-600" title="Histórico de Paradas" bodyClassName="p-5">
      {!stops.length ? (
        <p className="text-sm text-slate-500">Nenhuma parada disponível.</p>
      ) : (
        <ol className="relative ml-2 space-y-6 border-l-2 border-slate-100">
          {stops.map((stop) => {
            const done = stop.status === "CONCLUIDO" || stop.status === "CONCLUIDA"
            const completedAt = stop.completedAt ?? stop.actions.find((action) => action.completedAt)?.completedAt
            const deadline = earliest(stop.actions.filter((action) => !action.completed).map((action) => action.deadline))
            const controls = renderStopActions?.(stop)
            return (
              <li key={stop.id} className="relative pl-6">
                <span className={`absolute -left-[9px] top-0.5 h-4 w-4 rounded-full border-4 border-white ${dotColor(stop.status)}`} />
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0 space-y-0.5">
                    <p className="text-sm font-bold text-slate-800">{stop.city}</p>
                    <p className={`text-xs font-bold ${stop.status === "EM_TRANSITO" ? "text-amber-600" : "text-slate-500"}`}>
                      {stopStatusLabel(stop.status)} • {stop.actions.map(actionLabel).filter(Boolean).join(", ") || "Sem ações"}
                    </p>
                    <p className="text-xs text-slate-500">
                      {done ? `Realizado: ${formatTripDate(completedAt)}` : `Prazo: ${formatTripDate(deadline)}`}
                    </p>
                  </div>
                  {controls && <div className="shrink-0">{controls}</div>}
                </div>
              </li>
            )
          })}
        </ol>
      )}
    </TripCard>
  )
}
