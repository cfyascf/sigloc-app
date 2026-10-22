import { MapPin } from "lucide-react"

import DriverLocationMap from "@/components/driver-location-map"
import { TripCard } from "@/components/trips/trip-card"
import { formatTripDate } from "@/lib/trip-formatters"

/** Driver position on the map with the last ping time; `children` render below it (e.g. the carrier's ping button). */
export function TripLocationCard({ trip, children }) {
  return (
    <TripCard
      icon={MapPin}
      iconClassName="text-rose-500"
      title="Contexto Geográfico"
      aside={<span className="text-[10px] font-bold text-slate-400">Último ping: <span className="font-mono">{formatTripDate(trip?.lastPing)}</span></span>}
      className="overflow-hidden"
      bodyClassName=""
    >
      <DriverLocationMap latitude={trip?.latitude} longitude={trip?.longitude} label={trip?.driver?.name || trip?.plate} />
      {trip?.latitude != null && trip?.longitude != null && (
        <p className="border-t border-slate-100 px-4 py-2 font-mono text-[10px] text-slate-400">{trip.latitude.toFixed(4)}, {trip.longitude.toFixed(4)}</p>
      )}
      {children && <div className="space-y-2 border-t border-slate-100 p-4">{children}</div>}
    </TripCard>
  )
}
