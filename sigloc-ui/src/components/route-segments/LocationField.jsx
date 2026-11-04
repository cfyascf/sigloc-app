/* eslint-disable react/prop-types */
import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"
import { LOCATION_MODE, parseCoordinatePair } from "@/lib/locations"

const MODES = [
  { value: LOCATION_MODE.ADDRESS, label: "Endereço" },
  { value: LOCATION_MODE.COORDINATES, label: "Coordenadas" },
]

/**
 * Pickup or delivery point of a segment: an address, or latitude and longitude for places without
 * one. Pasting a pair copied from Google Maps ("-25.4284, -49.2733") into either coordinate input
 * fills both.
 */
export function LocationField({
  id,
  label,
  labelClassName,
  value,
  onChange,
  error,
  placeholder,
  inputClassName,
  errorClassName = "text-[11px] font-medium text-rose-600",
  disabled = false,
}) {
  const coordinates = value.mode === LOCATION_MODE.COORDINATES

  const setMode = (mode) => {
    if (mode === value.mode) return
    // A pair typed into the address box moves over to the coordinate inputs.
    const pair = mode === LOCATION_MODE.COORDINATES ? parseCoordinatePair(value.address) : null
    onChange({ ...value, mode, ...(pair ?? {}) })
  }

  const handlePaste = (event) => {
    const pair = parseCoordinatePair(event.clipboardData.getData("text"))
    if (!pair) return
    event.preventDefault()
    onChange({ ...value, ...pair })
  }

  const coordinateInput = (field, tag, example) => (
    <div className="relative">
      <span className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-[10px] font-bold tracking-wider text-slate-400 uppercase">
        {tag}
      </span>
      <Input
        id={`${id}-${field}`}
        aria-label={`${label}: ${field === "latitude" ? "latitude" : "longitude"}`}
        inputMode="decimal"
        disabled={disabled}
        value={value[field]}
        placeholder={example}
        onPaste={handlePaste}
        onChange={(event) => onChange({ ...value, [field]: event.target.value })}
        className={cn("pl-12 font-mono", inputClassName)}
      />
    </div>
  )

  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between gap-2">
        <label htmlFor={coordinates ? `${id}-latitude` : id} className={labelClassName}>
          {label}
        </label>
        <div role="radiogroup" aria-label={`${label}: forma de informar`} className="flex rounded-md bg-slate-100 p-0.5">
          {MODES.map((mode) => (
            <button
              key={mode.value}
              type="button"
              role="radio"
              aria-checked={value.mode === mode.value}
              disabled={disabled}
              onClick={() => setMode(mode.value)}
              className={cn(
                "rounded px-2 py-0.5 text-[10px] font-semibold transition-colors",
                value.mode === mode.value ? "bg-white text-slate-800 shadow-sm" : "text-slate-500 hover:text-slate-700"
              )}
            >
              {mode.label}
            </button>
          ))}
        </div>
      </div>

      {coordinates ? (
        <>
          <div className="grid grid-cols-2 gap-2">
            {coordinateInput("latitude", "Lat", "-25.4284")}
            {coordinateInput("longitude", "Long", "-49.2733")}
          </div>
          <p className="text-[11px] text-slate-400">Cole as coordenadas do Google Maps, ex.: -25.4284, -49.2733.</p>
        </>
      ) : (
        <Input
          id={id}
          disabled={disabled}
          value={value.address}
          placeholder={placeholder}
          onChange={(event) => onChange({ ...value, address: event.target.value })}
          className={inputClassName}
        />
      )}

      {error ? <p className={errorClassName}>{error}</p> : null}
    </div>
  )
}

export default LocationField
