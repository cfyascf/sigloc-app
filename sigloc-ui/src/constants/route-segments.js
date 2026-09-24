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
  IN_TRANSIT: "IN_TRANSIT",
  COMPLETED: "COMPLETED",
}

export const SEGMENT_STATUS_OPTIONS = [
  { value: SEGMENT_STATUS.AVAILABLE, label: "Disponível" },
  { value: SEGMENT_STATUS.ROUTED, label: "Roteirizado" },
  { value: SEGMENT_STATUS.IN_TRANSIT, label: "Em Trânsito" },
  { value: SEGMENT_STATUS.COMPLETED, label: "Concluído" },
]

/** Only `AVAILABLE` segments can be edited or deleted (enforced by the backend). */
export function isSegmentEditable(status) {
  return status === SEGMENT_STATUS.AVAILABLE
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
