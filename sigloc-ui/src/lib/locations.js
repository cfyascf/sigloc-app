/**
 * A segment end typed as an address or given as coordinates (farms, yards and quarries often have
 * no address). The API stores a coordinate as its address text in a fixed form,
 * "-25.428400, -49.273300" (latitude first, like Google Maps), which is how the forms reopen it.
 */

export const LOCATION_MODE = { ADDRESS: "address", COORDINATES: "coordinates" }

const STORED_COORDINATE = /^\s*(-?\d{1,3}(?:\.\d+)?)\s*,\s*(-?\d{1,3}(?:\.\d+)?)\s*$/

/** Form model for a stored address text: coordinates mode when it is a stored coordinate. */
export function locationFromText(text) {
  const match = STORED_COORDINATE.exec(text ?? "")
  return match
    ? { mode: LOCATION_MODE.COORDINATES, address: "", latitude: String(Number(match[1])), longitude: String(Number(match[2])) }
    : { mode: LOCATION_MODE.ADDRESS, address: text ?? "", latitude: "", longitude: "" }
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
