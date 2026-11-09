/**
 * A segment end typed as an address or given as coordinates (farms, yards and quarries often have
 * no address). The API stores a coordinate as its address text in a fixed form,
 * "-25.428400, -49.273300" (latitude first, like Google Maps), which is how the forms reopen it.
 */

export const LOCATION_MODE = { ADDRESS: "address", COORDINATES: "coordinates" }

const STORED_COORDINATE = /^\s*(-?\d{1,3}(?:\.\d+)?)\s*,\s*(-?\d{1,3}(?:\.\d+)?)\s*$/

/**
 * Form model for a stored address text: coordinates mode when it is a stored coordinate. `place` is
 * the city the saved address resolved to; it counts as already confirmed, so reopening a segment
 * neither looks the address up again nor asks for confirmation.
 */
export function locationFromText(text, place = null) {
  const match = STORED_COORDINATE.exec(text ?? "")
  if (match) {
    return { mode: LOCATION_MODE.COORDINATES, address: "", latitude: String(Number(match[1])), longitude: String(Number(match[2])) }
  }
  const address = text ?? ""
  const lookup = place && address.trim() ? { address: address.trim(), found: true, place, confirmed: true } : null
  return { mode: LOCATION_MODE.ADDRESS, address, latitude: "", longitude: "", lookup }
}

export const emptyLocation = () => locationFromText("")

/** "-25,4284" or "-25.4284" → -25.4284; null when it isn't a number. */
export function parseDecimal(value) {
  const text = String(value ?? "").trim().replace(",", ".")
  return /^-?\d+(\.\d+)?$/.test(text) ? Number(text) : null
}

/**
 * A pasted latitude/longitude pair, latitude first: "-25.4284, -49.2733" (Google Maps),
 * "-25.4284;-49.2733" or with comma decimals "-25,4284 -49,2733". Null for anything else.
 */
export function parseCoordinatePair(text) {
  const value = String(text ?? "").trim()
  const match =
    /^(-?\d+(?:\.\d+)?)\s*[,;\s]\s*(-?\d+(?:\.\d+)?)$/.exec(value) ??
    /^(-?\d+(?:,\d+)?)\s*[;\s]\s*(-?\d+(?:,\d+)?)$/.exec(value)
  return match ? { latitude: match[1].replace(",", "."), longitude: match[2].replace(",", ".") } : null
}

/**
 * Request fields for one end: the address text, or the coordinate (the API checks the range and
 * that it lies in Brazil). A plain string is taken as an address.
 */
export function toLocationRequest(value) {
  if (value == null || typeof value === "string") {
    return { text: value?.trim() || null, coordinate: null }
  }
  if (value.mode === LOCATION_MODE.COORDINATES) {
    return {
      text: null,
      coordinate: { latitude: parseDecimal(value.latitude), longitude: parseDecimal(value.longitude) },
    }
  }
  return { text: value.address?.trim() || null, coordinate: null }
}

/*
 * Address check before saving. The geocoder always returns its best guess, and a street name alone
 * can land in a city nobody typed ("Rua Inexistente" resolves to São Paulo). The location field looks
 * the address up and keeps the result in `location.lookup` ({ address, found, place, confirmed });
 * when the city found doesn't appear in the text, the contractor confirms it before the save.
 */

export const PLACE_CONFIRMATION_ERROR = "Confirme a cidade encontrada ou corrija o endereço."

// Lower case, no accents, punctuation as spaces: "São Paulo, SP" -> "sao paulo sp".
const normalizeText = (text) =>
  String(text ?? "")
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .trim()

/** Whether the city of a geocoded "City, UF" appears in the typed address. */
export function placeMatchesTyped(place, typed) {
  const city = normalizeText(String(place ?? "").split(",")[0])
  return !city || ` ${normalizeText(typed)} `.includes(` ${city} `)
}

/** The lookup made for the address as it is now typed; null when there is none or it is stale. */
export function currentLookup(location) {
  if (location?.mode !== LOCATION_MODE.ADDRESS) return null
  const lookup = location.lookup
  return lookup && lookup.address === (location.address ?? "").trim() ? lookup : null
}

/** The address resolved to a city that wasn't typed, and the contractor hasn't confirmed it yet. */
export function needsPlaceConfirmation(location) {
  const lookup = currentLookup(location)
  return Boolean(lookup?.found && lookup.place && !lookup.confirmed && !placeMatchesTyped(lookup.place, location.address))
}
