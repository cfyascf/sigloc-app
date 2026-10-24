import { segmentShortId } from "@/constants/route-segments"
import { formatDeadline } from "@/lib/deadline-formatters"

/** First comma-separated part of a "City, UF" (or free-form) value. */
export const cityLabel = (value) => (value ? value.split(",")[0].trim() : "—")

/**
 * Extracts the neighbourhood (bairro) from a typed full address, so two stops in
 * the same city can still be told apart. Brazilian addresses are usually written
 * "Rua X, 100, Bairro, Cidade, UF"; the neighbourhood is the comma-separated part
 * right before the city. When the city can't be located in the address, falls
 * back to the second-to-last part, and finally to the whole address.
 */
export const neighborhoodLabel = (fullAddress, cityValue) => {
  if (!fullAddress) {
    return cityLabel(cityValue)
  }

  const parts = fullAddress
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean)

  if (parts.length === 0) {
    return cityLabel(cityValue)
  }

  const city = cityLabel(cityValue)
  const cityIndex = parts.findIndex(
    (part) => part.toLowerCase() === city.toLowerCase()
  )

  if (cityIndex > 0) {
    return parts[cityIndex - 1]
  }

  return parts.length >= 2 ? parts[parts.length - 2] : parts[0]
}

/**
 * Builds a de-duplicated, ordered itinerary from the resolved segments.
 *
 * Stops are grouped by location so a shared city across segments collapses into a
 * single node (the "Milking Run" consolidation). When a single segment's origin
 * and destination fall in the same city, the two endpoints are kept as separate
 * nodes titled by their neighbourhood, so the leg is never shown as one point.
 */
export const buildUnifiedTimeline = (segments) => {
  const nodes = []
  const addAction = (key, title, action, deadline) => {
    const deadlineTime = toTime(deadline)
    const existing = nodes.find((n) => n.key === key)
    if (existing) {
      existing.actions.push(action)
      // A consolidated node keeps the tightest (earliest) deadline, since that is
      // the binding constraint for the carrier at that stop.
      if (deadlineTime !== null && (existing.deadlineTime === null || deadlineTime < existing.deadlineTime)) {
        existing.deadlineTime = deadlineTime
        existing.deadline = deadline
      }
      return
    }
    nodes.push({ key, title, actions: [action], deadline, deadlineTime })
  }

  segments.forEach((s) => {
    const label = s.items?.[0]?.name ?? "Carga"
    const ref = segmentShortId(s.id)
    const originCity = cityLabel(s.originCity ?? s.origin)
    const destinationCity = cityLabel(s.destinationCity ?? s.destination)
    const sameCity = originCity === destinationCity

    const originTitle = sameCity
      ? neighborhoodLabel(s.origin, s.originCity ?? s.origin)
      : originCity
    const destinationTitle = sameCity
      ? neighborhoodLabel(s.destination, s.destinationCity ?? s.destination)
      : destinationCity

    // For an intra-city leg keep the pickup and drop-off as two distinct nodes
    // (keyed per segment/role) so they never collapse into a single point, even
    // when their neighbourhoods can't be told apart. Inter-city stops stay keyed
    // by city so a shared city across segments still consolidates.
    addAction(
      sameCity ? `${s.id}::origin` : originCity,
      originTitle,
      `Coleta · ${label} · ${ref}`,
      s.pickupDeadline
    )
    addAction(
      sameCity ? `${s.id}::destination` : destinationCity,
      destinationTitle,
      `Descarga · ${label} · ${ref}`,
      s.deliveryDeadline
    )
  })

  return nodes.map((node) => ({
    city: node.title,
    actions: node.actions,
    ...(node.deadlineTime !== null ? { time: formatDeadline(node.deadline) } : {}),
  }))
}

function toTime(value) {
  if (!value) {
    return null
  }
  const time = new Date(value).getTime()
  return Number.isNaN(time) ? null : time
}
