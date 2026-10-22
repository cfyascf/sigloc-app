import { Clock, ListChecks } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { TripCard } from "@/components/trips/trip-card"
import { formatTripDate } from "@/lib/trip-formatters"

export function TripEventLog({ events }) {
  return (
    <TripCard icon={ListChecks} iconClassName="text-indigo-600" title="Log de Eventos">
      {!events.length ? (
        <p className="text-sm text-slate-500">Nenhum evento registrado.</p>
      ) : (
        <ul className="space-y-2">
          {events.map((event) => (
            <li key={event.id} className="flex items-center gap-3 rounded-lg border border-slate-100 bg-slate-50 p-3 text-sm">
              <Clock className="shrink-0 text-slate-400" size={16} />
              <span className="shrink-0 font-mono text-xs font-medium text-slate-600">{formatTripDate(event.timestamp)}</span>
              <Badge variant="secondary" className="h-auto whitespace-normal border-none bg-indigo-100 text-left text-indigo-700">{event.message}</Badge>
            </li>
          ))}
        </ul>
      )}
    </TripCard>
  )
}
