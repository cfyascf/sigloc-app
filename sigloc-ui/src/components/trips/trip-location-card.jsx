import { MapPin } from "lucide-react"

import DriverLocationMap from "@/components/driver-location-map"
import { TripCard } from "@/components/trips/trip-card"
import { formatTripDate } from "@/lib/trip-formatters"

/**
 * Driver position on the map with the last ping time; `children` render below it (e.g. the carrier's ping button).
 * Pass `className` to size the card and `mapClassName` to let the map area flex within it.
 */
export function TripLocationCard({ trip, children, className = "overflow-hidden", mapClassName = "h-[260px]" }) {
  return (
    <TripCard
      icon={MapPin}
      iconClassName="text-rose-500"
      title="Contexto Geográfico"
      aside={<span className="text-[10px] font-bold text-slate-400">Último ping: <span className="font-mono">{formatTripDate(trip?.lastPing)}</span></span>}
      className={className}
      bodyClassName="flex min-h-0 flex-1 flex-col"
    >
      <DriverLocationMap latitude={trip?.latitude} longitude={trip?.longitude} label={trip?.driver?.name || trip?.plate} className={mapClassName} />
      {trip?.latitude != null && trip?.longitude != null && (
        <p className="border-t border-slate-100 px-4 py-2 font-mono text-[10px] text-slate-400">{trip.latitude.toFixed(4)}, {trip.longitude.toFixed(4)}</p>
      )}
      {children && <div className="space-y-2 border-t border-slate-100 p-4">{children}</div>}
    </TripCard>
  )
}
