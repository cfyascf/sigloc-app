/**
 * Shared formatting and ordering helpers for route-segment pickup/delivery
 * deadlines, used by the segment list, the offer-freight travel plan and its
 * route-level summary so the three screens stay consistent.
 */

const DATE_TIME_FORMAT = new Intl.DateTimeFormat("pt-BR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
})

/**
 * "DD/MM/YYYY HH:mm", or "—" when the value is missing/invalid. Built from parts
 * so the date/time separator is always a single space, independent of the ICU
 * build's locale punctuation.
 */
export function formatDeadline(value) {
  if (!value) {
    return "—"
  }

  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) {
    return "—"
  }

  const parts = DATE_TIME_FORMAT.formatToParts(date)
  const part = (type) => parts.find((p) => p.type === type)?.value ?? ""

  return `${part("day")}/${part("month")}/${part("year")} ${part("hour")}:${part("minute")}`
}

/** Earliest pickup deadline across the segments, or null when none is set. */
export function earliestPickup(segments = []) {
  return minDate(segments.map((s) => s?.pickupDeadline))
}

/** Latest delivery deadline across the segments, or null when none is set. */
export function latestDelivery(segments = []) {
  return maxDate(segments.map((s) => s?.deliveryDeadline))
}

/**
 * Detects segments whose deadlines are out of order for a sequential milking
 * run: a segment that must be picked up only after a later segment is already
 * due for delivery. Returns true when at least one such overlap exists, so the
 * UI can surface a non-blocking warning. Segments missing a deadline are
 * ignored (the backend already requires them on create).
 */
export function hasDeadlineOrderingConflict(segments = []) {
  const ordered = segments
    .filter((s) => s?.pickupDeadline && s?.deliveryDeadline)
    .map((s) => ({
      pickup: new Date(s.pickupDeadline).getTime(),
      delivery: new Date(s.deliveryDeadline).getTime(),
    }))
    .filter((s) => !Number.isNaN(s.pickup) && !Number.isNaN(s.delivery))
    .sort((a, b) => a.pickup - b.pickup)

  for (let i = 1; i < ordered.length; i += 1) {
    if (ordered[i].pickup < ordered[i - 1].delivery) {
      return true
    }
  }

  return false
}

function toTime(value) {
  if (!value) {
    return null
  }
  const time = new Date(value).getTime()
  return Number.isNaN(time) ? null : time
}

function minDate(values) {
  const times = values.map(toTime).filter((t) => t !== null)
  return times.length ? new Date(Math.min(...times)) : null
}

function maxDate(values) {
  const times = values.map(toTime).filter((t) => t !== null)
  return times.length ? new Date(Math.max(...times)) : null
}
