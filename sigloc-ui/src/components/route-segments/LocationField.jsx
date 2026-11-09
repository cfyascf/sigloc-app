/* eslint-disable react/prop-types */
import { useEffect, useRef, useState } from "react"
import { AlertTriangle, MapPin } from "lucide-react"

import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"
import {
  LOCATION_MODE,
  currentLookup,
  needsPlaceConfirmation,
  parseCoordinatePair,
  placeMatchesTyped,
} from "@/lib/locations"
import { locateAddress } from "@/services/route-segment-service"

// Look the address up once typing pauses; shorter texts can't name a place.
const LOOKUP_DELAY_MS = 700
const MIN_LOOKUP_LENGTH = 3

const MODES = [
  { value: LOCATION_MODE.ADDRESS, label: "Endereço" },
  { value: LOCATION_MODE.COORDINATES, label: "Coordenadas" },
]

/**
 * Pickup or delivery point of a segment: an address, or latitude and longitude for places without
 * one. Pasting a pair copied from Google Maps ("-25.4284, -49.2733") into either coordinate input
 * fills both.
 *
 * A typed address is looked up as soon as typing pauses, and the field shows the city it resolved
 * to. When that city doesn't appear in the text (a bare street name can land anywhere), the
 * contractor confirms it here before the page lets the segment be saved.
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

  // The lookup resolves after the render that started it: read the latest value and handler.
  const latest = useRef({ value, onChange })
  useEffect(() => {
    latest.current = { value, onChange }
  })

  // Text whose lookup failed (provider down): not retried, the save geocodes it anyway.
  const [failedText, setFailedText] = useState(null)
  const text = coordinates ? "" : (value.address ?? "").trim()
  const lookup = currentLookup(value)
  const shouldLookUp = !disabled && text.length >= MIN_LOOKUP_LENGTH && !lookup && failedText !== text

  useEffect(() => {
    if (!shouldLookUp) return undefined
    const controller = new AbortController()
    const timer = setTimeout(async () => {
      try {
        const result = await locateAddress(text, { signal: controller.signal })
        const { value: current, onChange: emit } = latest.current
        if (current.mode !== LOCATION_MODE.ADDRESS || (current.address ?? "").trim() !== text) return
        emit({ ...current, lookup: { address: text, found: result.found, place: result.place, confirmed: false } })
      } catch (err) {
        if (err?.name !== "AbortError") setFailedText(text)
      }
    }, LOOKUP_DELAY_MS)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [shouldLookUp, text])

  const confirmPlace = () => onChange({ ...value, lookup: { ...lookup, confirmed: true } })

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
                value.mode === mode.value ? "bg-white text-slate-800" : "text-slate-500 hover:text-slate-700"
              )}
            >
              {mode.label}
            </button>
          ))}
        </div>
      </div>

      {coordinates ? (
        <div className="grid grid-cols-2 gap-2">
          {coordinateInput("latitude", "Lat", "-25.4284")}
          {coordinateInput("longitude", "Long", "-49.2733")}
        </div>
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

      {coordinates ? null : (
        <PlaceHint lookup={lookup} pending={shouldLookUp} typed={value.address} onConfirm={confirmPlace} disabled={disabled} />
      )}

      {error ? <p className={errorClassName}>{error}</p> : null}
    </div>
  )
}

/** Where the typed address landed: the city, a warning to confirm, or "not found". */
function PlaceHint({ lookup, pending, typed, onConfirm, disabled }) {
  if (pending) {
    return (
      <p aria-live="polite" className="text-[11px] text-slate-400">
        Localizando endereço…
      </p>
    )
  }
  if (!lookup) return null
  if (!lookup.found) {
    return (
      <p aria-live="polite" className="text-[11px] font-medium text-amber-700">
        Não encontramos este endereço. Inclua a cidade e a UF (ex.: Curitiba, PR).
      </p>
    )
  }
  if (!lookup.place) return null

  if (needsPlaceConfirmation({ mode: LOCATION_MODE.ADDRESS, address: typed, lookup })) {
    return (
      <div role="status" className="space-y-1.5 rounded-md border border-amber-200 bg-amber-50 p-2 text-[11px] text-amber-800">
        <p className="flex items-start gap-1.5">
          <AlertTriangle size={13} className="mt-px shrink-0" aria-hidden="true" />
          <span>
            Este endereço foi localizado em <strong>{lookup.place}</strong>, uma cidade que não aparece no que foi
            digitado. Confira antes de salvar, ou inclua a cidade e a UF no endereço.
          </span>
        </p>
        <button
          type="button"
          disabled={disabled}
          onClick={onConfirm}
          className="rounded border border-amber-300 bg-white px-2 py-0.5 font-semibold text-amber-800 hover:bg-amber-100"
        >
          É este o local
        </button>
      </div>
    )
  }

  const confirmedElsewhere = !placeMatchesTyped(lookup.place, typed)
  return (
    <p aria-live="polite" className="flex items-center gap-1 text-[11px] text-slate-500">
      <MapPin size={12} className={confirmedElsewhere ? "text-amber-600" : "text-emerald-600"} aria-hidden="true" />
      <span>
        Localizado em <strong className="font-semibold text-slate-700">{lookup.place}</strong>
        {confirmedElsewhere ? " (confirmado)" : ""}
      </span>
    </p>
  )
}

export default LocationField
