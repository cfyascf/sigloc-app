/**
 * Route segment (Trecho) domain constants — the single source of truth for the
 * enum wire values exchanged with the Sigloc API and their user-facing labels.
 *
 * The backend computes distance, travel time, physical totals and the
 * consolidated vehicle requirement server-side; the UI only sends the editable
 * fields (itinerary, deadlines, budget/toll, and the linked product items).
 */

import { TRANSPORT_ENVIRONMENT } from "./products.js"

/** Segment lifecycle wire values (see backend `SegmentEnumMappings`). */
export const SEGMENT_STATUS = {
  AVAILABLE: "AVAILABLE",
  ROUTED: "ROUTED",
  IN_EXECUTION: "IN_EXECUTION",
  COMPLETED: "COMPLETED",
}

export const SEGMENT_STATUS_OPTIONS = [
  { value: SEGMENT_STATUS.AVAILABLE, label: "Disponível" },
  { value: SEGMENT_STATUS.ROUTED, label: "Em leilão" },
  { value: SEGMENT_STATUS.IN_EXECUTION, label: "Em execução" },
  { value: SEGMENT_STATUS.COMPLETED, label: "Concluído" },
]

/** Only `AVAILABLE` segments can be edited or deleted (enforced by the backend). */
/**
 * Short code shown for a segment. Segment ids are time-ordered (UUIDv7), so segments created
 * close together share their first characters; the tail is random and stays distinct.
 */
export function segmentShortId(id) {
  return String(id).slice(-8)
}

export function isSegmentEditable(status) {
  return status === SEGMENT_STATUS.AVAILABLE
}

/**
 * Handling class of a load (same rule as the API's CargoClassification): dangerous, fragile and
 * common goods never travel together, inside a segment or across the segments of a route. A
 * product that is both dangerous and fragile counts as dangerous.
 */
export const CARGO_CLASS = { COMUM: "COMUM", FRAGIL: "FRAGIL", PERIGOSA: "PERIGOSA", MISTA: "MISTA" }

const CARGO_CLASS_NAMES = { COMUM: "carga comum", FRAGIL: "carga frágil", PERIGOSA: "carga perigosa" }

export const CARGO_CLASS_BADGES = {
  // Same colours as the product badges in the catalogue (Hazmat amber, Frágil rose).
  PERIGOSA: { label: "Perigosa", className: "bg-amber-100 text-amber-700" },
  FRAGIL: { label: "Frágil", className: "bg-rose-50 text-rose-600" },
  MISTA: { label: "Carga mista", className: "bg-slate-200 text-slate-700" },
}

export function cargoClassOfProduct(product) {
  if (product?.dangerous) return CARGO_CLASS.PERIGOSA
  if (product?.fragile) return CARGO_CLASS.FRAGIL
  return CARGO_CLASS.COMUM
}

export function cargoClassName(cargoClass) {
  return CARGO_CLASS_NAMES[cargoClass] ?? "carga comum"
}

/**
 * Why a segment can't join the route being selected, or null when it can. `selectedClass` is the
 * class of the segments already ticked (null when none is); `selectedRange` is the temperature
 * range every ticked segment tolerates (null when none is ticked).
 */
export function cargoSelectionBlock(segment, selectedClass, selectedRange = null) {
  if (segment.cargoClass === CARGO_CLASS.MISTA) {
    return "Este trecho mistura classes de carga. Edite o trecho e remova os produtos de outra classe antes de ofertá-lo."
  }
  if (selectedClass && segment.cargoClass !== selectedClass) {
    return `Uma rota não pode misturar ${cargoClassName(selectedClass)} com ${cargoClassName(segment.cargoClass)}.`
  }
  const segmentRange = segmentTemperatureRange(segment)
  if (selectedRange && segmentRange && isEmptyRange(intersectTemperatureRanges([selectedRange, segmentRange]))) {
    return `Uma rota precisa viajar a uma única temperatura: este trecho exige ${formatTemperatureRange(segmentRange)} e os trechos selecionados, ${formatTemperatureRange(selectedRange)}.`
  }
  return null
}

/**
 * Same rule as the API's TemperatureCompatibility: a compartment runs at a single temperature, so
 * a route only joins segments whose ranges overlap. Dry products travel at ambient temperature.
 */
const AMBIENT_RANGE = { min: 0, max: 35 }

function productTemperatureRange(item) {
  if (!item.transportEnvironment || item.transportEnvironment === TRANSPORT_ENVIRONMENT.DRY) {
    return AMBIENT_RANGE
  }
  return { min: item.tempMin ?? -Infinity, max: item.tempMax ?? Infinity }
}

export function intersectTemperatureRanges(ranges) {
  const present = ranges.filter(Boolean)
  if (present.length === 0) return null
  return {
    min: Math.max(...present.map((range) => range.min)),
    max: Math.min(...present.map((range) => range.max)),
  }
}

const isEmptyRange = (range) => Boolean(range) && range.min > range.max

/** Temperature range every product of the segment tolerates, or null when it has no products. */
export function segmentTemperatureRange(segment) {
  return intersectTemperatureRanges((segment?.items ?? []).map(productTemperatureRange))
}

const degreesFormat = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })
const degrees = (value) => `${degreesFormat.format(value)} °C`

/** "-18 °C a -12 °C", "a partir de 2 °C", "até 8 °C" or "qualquer temperatura". */
export function formatTemperatureRange(range) {
  if (!range) return "qualquer temperatura"
  const hasMin = Number.isFinite(range.min)
  const hasMax = Number.isFinite(range.max)
  if (hasMin && hasMax) return `${degrees(range.min)} a ${degrees(range.max)}`
  if (hasMin) return `a partir de ${degrees(range.min)}`
  if (hasMax) return `até ${degrees(range.max)}`
  return "qualquer temperatura"
}

/**
 * What still fits the route being selected, e.g. "Para compor esta rota, selecione apenas trechos
 * de carga frágil que possam viajar entre 2 °C e 8 °C."
 */
export function selectionCompatibilityHint(selectedClass, selectedRange) {
  if (!selectedClass) return null
  const hasMin = Number.isFinite(selectedRange?.min)
  const hasMax = Number.isFinite(selectedRange?.max)
  const temperature =
    hasMin && hasMax
      ? ` que possam viajar entre ${degrees(selectedRange.min)} e ${degrees(selectedRange.max)}`
      : hasMin || hasMax
        ? ` que possam viajar ${formatTemperatureRange(selectedRange)}`
        : ""
  return `Para compor esta rota, selecione apenas trechos de ${cargoClassName(selectedClass)}${temperature}.`
}

/**
 * An available segment whose pickup deadline has passed. It stays editable (the contractor can
 * move the deadline) but can't go into a new auction: the backend rejects past pickups.
 */
export function isPickupOverdue(segment, now = Date.now()) {
  if (segment?.status !== SEGMENT_STATUS.AVAILABLE || !segment.pickupDeadline) return false
  return new Date(segment.pickupDeadline).getTime() <= now
}

function findLabel(options, value) {
  return options.find((option) => option.value === value)?.label ?? value
}

export const segmentStatusLabel = (value) =>
  value ? findLabel(SEGMENT_STATUS_OPTIONS, value) : ""

/**
 * Maps backend validation field names to the form field keys used by the route
 * segment screens, so inline errors land on the right input. The backend uses
 * camelCase field names already (`origin`, `destination`, `pickupDeadline`,
 * `deliveryDeadline`, `budgetCeiling`, `estimatedTollCost`, `items`) and indexed
 * item paths such as `items[0].productId` / `items[0].quantity`.
 */
export const ROUTE_SEGMENT_FIELD_MAP = {
  origin: "origin",
  destination: "destination",
  pickupDeadline: "pickupDeadline",
  deliveryDeadline: "deliveryDeadline",
  budgetCeiling: "budgetCeiling",
  estimatedTollCost: "estimatedTollCost",
  items: "items",
}

/**
 * Remaps a `{ [backendField]: message }` error object to the form field keys
 * used by the route segment screens. Unknown keys (including indexed
 * `items[i].*` paths) are collapsed onto the generic `items` field so the user
 * still sees the message near the product list.
 */
export function mapRouteSegmentFieldErrors(fieldErrors) {
  if (!fieldErrors) {
    return {}
  }

  const mapped = {}
  for (const [field, message] of Object.entries(fieldErrors)) {
    let key = ROUTE_SEGMENT_FIELD_MAP[field]

    if (!key) {
      key = field.startsWith("items") ? "items" : field
    }

    if (!mapped[key]) {
      mapped[key] = message
    }
  }
  return mapped
}
