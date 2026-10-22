const ICON_TONES = {
  blue: "bg-blue-50 text-blue-600",
  indigo: "bg-indigo-50 text-indigo-600",
  purple: "bg-purple-50 text-purple-600",
  emerald: "bg-emerald-50 text-emerald-600",
  amber: "bg-amber-100 text-amber-600",
  rose: "bg-rose-100 text-rose-600",
}

const ALERT_SURFACES = {
  amber: "border-amber-200 bg-amber-50/50",
  rose: "border-rose-200 bg-rose-50/50",
}

/** Top-bar KPI. `alert` ("amber" | "rose") tints the whole card for SLA attention or delay. */
export function TripKpiCard({ icon: Icon, label, value, hint, tone = "blue", alert }) {
  return (
    <div className={`flex items-center gap-3 rounded-xl border p-4 transition-colors ${ALERT_SURFACES[alert] ?? "border-slate-200 bg-white"}`}>
      <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${ICON_TONES[alert ?? tone]}`}>
        <Icon size={20} />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</p>
        <p className="truncate text-sm font-bold text-slate-900">{value}</p>
        {hint && <p className="truncate text-[11px] font-medium text-slate-500">{hint}</p>}
      </div>
    </div>
  )
}
