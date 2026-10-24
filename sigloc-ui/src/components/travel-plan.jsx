import { MapPinned } from "lucide-react"

import { cn } from "@/lib/utils"

function getStopBadgeClass(index, totalStops) {
  if (index === 0) {
    return "bg-slate-800"
  }

  if (index === totalStops - 1) {
    return "bg-emerald-500"
  }

  return "bg-blue-500"
}

/**
 * "Plano de Viagem (Milking Run)" card.
 * Each stop: { city, action?: string, actions?: string[], time?: string }.
 */
export default function TravelPlan({ stops = [], totalDistanceKm, emptyMessage, className }) {
  return (
    <div className={cn("flex flex-col rounded-xl border border-slate-200 bg-white", className)}>
      <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/50 px-5 py-3">
        <div className="flex items-center gap-2">
          <MapPinned size={16} className="text-emerald-600" />
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Plano de Viagem (Milking Run)</h2>
        </div>
        {totalDistanceKm != null ? (
          <span className="text-xs font-mono font-bold text-slate-400 bg-white px-2 py-1 rounded border border-slate-200">{totalDistanceKm} km totais</span>
        ) : null}
      </div>

      {stops.length === 0 ? (
        <div className="p-5 py-10 text-center text-sm text-slate-500">
          {emptyMessage ?? "Nenhuma parada disponível."}
        </div>
      ) : (
        <div className="overflow-x-auto">
          <div className="p-5 flex flex-col md:flex-row gap-6 md:gap-2 justify-between">
            {stops.map((stop, index) => {
              const actions = stop.actions ?? (stop.action ? [stop.action] : [])

              return (
                <div key={`${stop.city}-${index}`} className="flex-1 relative md:min-w-[140px]">
                  {/* Linha conectora (Some no mobile) */}
                  {index !== stops.length - 1 && (
                    <div className="hidden md:block absolute top-3 left-[50%] w-full border-t-2 border-dashed border-slate-200" />
                  )}

                  <div className="relative z-10 flex flex-col items-center text-center">
                    <div className={`h-6 w-6 rounded-full border-4 border-white flex items-center justify-center text-[10px] font-black text-white ${getStopBadgeClass(index, stops.length)}`}>
                      {index + 1}
                    </div>
                    <p className="text-xs font-bold text-slate-800 mt-2">{stop.city}</p>
                    {actions.map((action) => (
                      <p key={action} className="text-[10px] font-semibold text-slate-500">{action}</p>
                    ))}
                    {stop.time ? (
                      <p className="text-[10px] font-bold text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded mt-1">{stop.time}</p>
                    ) : null}
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
