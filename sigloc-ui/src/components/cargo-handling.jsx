import { Flame, GlassWater, Snowflake, Thermometer } from "lucide-react"

import { TRANSPORT_ENVIRONMENT } from "@/constants/products"
import { formatTemperatureRange, segmentTemperatureRange } from "@/constants/route-segments"

/** Special handling a product can require; the list row and the expanded details share these icons. */
export const HANDLING = {
  dangerous: {
    label: "Perigosa",
    description: "Carga perigosa: exige motorista com MOPP e não viaja com carga frágil ou comum.",
    Icon: Flame,
    badgeClassName: "bg-amber-100 text-amber-700",
  },
  fragile: {
    label: "Frágil",
    description: "Carga frágil: exige fixação de carga e não viaja com carga perigosa ou comum.",
    Icon: GlassWater,
    badgeClassName: "bg-rose-50 text-rose-600",
  },
  chilled: {
    label: "Refrigerado",
    description: "Carga refrigerada (temperatura positiva controlada).",
    Icon: Thermometer,
    badgeClassName: "bg-sky-50 text-sky-700",
  },
  frozen: {
    label: "Congelado",
    description: "Carga congelada (temperatura negativa controlada).",
    Icon: Snowflake,
    badgeClassName: "bg-indigo-50 text-indigo-700",
  },
}

const HANDLING_ORDER = ["dangerous", "fragile", "chilled", "frozen"]

export function itemHandlingKeys(item) {
  return HANDLING_ORDER.filter((key) => {
    if (key === "dangerous") return item.dangerous
    if (key === "fragile") return item.fragile
    if (key === "chilled") return item.transportEnvironment === TRANSPORT_ENVIRONMENT.CHILLED
    return item.transportEnvironment === TRANSPORT_ENVIRONMENT.FROZEN
  })
}

export function segmentHandlingKeys(segment) {
  const keys = new Set((segment.items ?? []).flatMap(itemHandlingKeys))
  return HANDLING_ORDER.filter((key) => keys.has(key))
}

/** Compact, icon-only summary of a segment's special handling for the list row. */
export function SegmentHandlingIcons({ segment }) {
  const keys = segmentHandlingKeys(segment)
  const temperature = formatTemperatureRange(segmentTemperatureRange(segment))

  if (keys.length === 0) {
    return (
      <span
        title={`Carga comum, sem manuseio especial. Temperatura do trecho: ${temperature}.`}
        className="text-[10px] font-semibold uppercase tracking-wide text-slate-400"
      >
        Comum
      </span>
    )
  }

  return (
    <div className="flex items-center gap-1.5">
      {keys.map((key) => {
        const { label, description, Icon, badgeClassName } = HANDLING[key]
        const isTemperature = key === "chilled" || key === "frozen"
        const tooltip = `${label}: ${description}${isTemperature ? ` Temperatura do trecho: ${temperature}.` : ""}`
        return (
          <span
            key={key}
            role="img"
            aria-label={tooltip}
            title={tooltip}
            className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full ${badgeClassName}`}
          >
            <Icon size={14} />
          </span>
        )
      })}
    </div>
  )
}
