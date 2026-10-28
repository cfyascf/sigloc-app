/**
 * Route segment (Trecho) domain constants — the single source of truth for the
 * enum wire values exchanged with the Sigloc API and their user-facing labels.
 *
 * The backend computes distance, travel time, physical totals and the
 * consolidated vehicle requirement server-side; the UI only sends the editable
 * fields (itinerary, deadlines, budget/toll, and the linked product items).
 */

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
 * class of the segments already ticked (null when none is).
 */
export function cargoSelectionBlock(segment, selectedClass) {
  if (segment.cargoClass === CARGO_CLASS.MISTA) {
    return "Este trecho mistura classes de carga. Edite o trecho e remova os produtos de outra classe antes de ofertá-lo."
  }
  if (selectedClass && segment.cargoClass !== selectedClass) {
    return `Uma rota não pode misturar ${cargoClassName(selectedClass)} com ${cargoClassName(segment.cargoClass)}.`
  }
  return null
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
