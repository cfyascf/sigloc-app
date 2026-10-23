import { useEffect } from "react"
import { MapContainer, TileLayer, Marker, Tooltip, useMap } from "react-leaflet"
import L from "leaflet"
import "leaflet/dist/leaflet.css"

const DEFAULT_ZOOM = 14

// OpenStreetMap's volunteer tile server blocks apps that don't follow its tile usage policy and
// CARTO's public raster tiles now require an API key, so the default is Esri's keyless street map.
// Override both values to use another provider (e.g. a keyed MapTiler/Stadia URL template).
const TILE_URL = import.meta.env.VITE_MAP_TILE_URL || "https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}"
const TILE_ATTRIBUTION = import.meta.env.VITE_MAP_TILE_ATTRIBUTION || "Tiles &copy; Esri &mdash; Source: Esri, HERE, Garmin, OpenStreetMap contributors"

// A divIcon avoids Leaflet's default marker PNGs, which bundlers fail to resolve.
const driverIcon = L.divIcon({
  className: "",
  html: '<div class="relative h-5 w-5"><span class="absolute inset-0 animate-ping rounded-full bg-rose-500 opacity-60"></span><span class="absolute inset-0 rounded-full border-2 border-white bg-rose-600 shadow"></span></div>',
  iconSize: [20, 20],
  iconAnchor: [10, 10],
})

// MapContainer only reads `center` on mount, so follow the driver on later updates.
function Recenter({ latitude, longitude }) {
  const map = useMap()
  useEffect(() => {
    map.setView([latitude, longitude], map.getZoom())
  }, [map, latitude, longitude])
  return null
}

export default function DriverLocationMap({ latitude, longitude, label, className = "h-[260px]" }) {
  if (latitude == null || longitude == null) {
    return (
      <div className={`flex items-center justify-center bg-slate-50 text-xs font-medium text-slate-500 ${className}`}>
        Sem posição para exibir no mapa.
      </div>
    )
  }

  const position = [latitude, longitude]

  return (
    // `isolate` keeps Leaflet's internal z-indexes from covering dialogs, menus and toasts.
    <div className={`isolate ${className}`}>
      <MapContainer center={position} zoom={DEFAULT_ZOOM} scrollWheelZoom={false} className="h-full w-full">
        <TileLayer
          attribution={TILE_ATTRIBUTION}
          url={TILE_URL}
          maxZoom={19}
          referrerPolicy="strict-origin-when-cross-origin"
        />
        <Marker position={position} icon={driverIcon}>
          <Tooltip direction="top" offset={[0, -10]}>
            {label || `${latitude.toFixed(4)}, ${longitude.toFixed(4)}`}
          </Tooltip>
        </Marker>
        <Recenter latitude={latitude} longitude={longitude} />
      </MapContainer>
    </div>
  )
}
