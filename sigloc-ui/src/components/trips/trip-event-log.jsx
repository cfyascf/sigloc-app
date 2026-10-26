import { Clock, ListChecks } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { TripCard } from "@/components/trips/trip-card"
import { eventSeverity, formatTripDate } from "@/lib/trip-formatters"

export function TripEventLog({ events, aside, className = "", bodyClassName = "p-4" }) {
  return (
    <TripCard icon={ListChecks} iconClassName="text-indigo-600" title="Log de Eventos" aside={aside} className={className} bodyClassName={bodyClassName}>
      {!events.length ? (
        <p className="text-sm text-slate-500">Nenhum evento registrado.</p>
      ) : (
        <ul className="h-full space-y-2 overflow-y-auto pr-1">
          {events.map((event) => {
            const severity = eventSeverity(event)
            return (
              <li key={event.id} className="flex items-center gap-3 rounded-lg border border-slate-100 bg-slate-50 p-3 text-sm">
                <Clock className="shrink-0 text-slate-400" size={16} />
                <span className="shrink-0 font-mono text-xs font-medium text-slate-600">{formatTripDate(event.timestamp)}</span>
                <Badge variant="secondary" className={`h-auto shrink-0 border-none text-[10px] font-bold uppercase tracking-wide ${severity.style}`}>{severity.label}</Badge>
                <span className="min-w-0 flex-1 text-left text-xs text-slate-700">{event.message}</span>
              </li>
            )
          })}
        </ul>
      )}
    </TripCard>
  )
}
