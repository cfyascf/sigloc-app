/** Validity of dated documents (RNTRC, exams…) against today's date in Brasília, as the API checks it. */

// The API flags a document as due when it expires within this many days.
export const EXPIRING_WITHIN_DAYS = 30

/** Today's date in Brasília as "yyyy-MM-dd". */
export function brasiliaToday(now = new Date()) {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "America/Sao_Paulo", year: "numeric", month: "2-digit", day: "2-digit" }).format(now)
}

/**
 * "expired" (before today), "expiring" (within 30 days) or "valid"; null without a date.
 * `days` is the number of days left (negative once expired).
 */
export function validityStatus(isoDate, today = brasiliaToday()) {
  if (!isoDate) return null
  const days = Math.round((Date.parse(`${isoDate}T00:00:00Z`) - Date.parse(`${today}T00:00:00Z`)) / 86_400_000)
  if (Number.isNaN(days)) return null
  if (days < 0) return { kind: "expired", days }
  if (days <= EXPIRING_WITHIN_DAYS) return { kind: "expiring", days }
  return { kind: "valid", days }
}

/** "09/11/2026" from "2026-11-09". */
export function formatIsoDate(isoDate) {
  const [year, month, day] = String(isoDate).split("-")
  return day && month && year ? `${day}/${month}/${year}` : isoDate
}
