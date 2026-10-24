import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let mod

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  mod = await server.ssrLoadModule("/src/lib/deadline-formatters.js")
})

after(async () => {
  await server?.close()
})

test("formatDeadline renders DD/MM/YYYY HH:mm and falls back to —", () => {
  const { formatDeadline } = mod
  assert.equal(formatDeadline("2026-03-05T14:30:00-03:00"), "05/03/2026 14:30")
  assert.equal(formatDeadline(null), "—")
  assert.equal(formatDeadline("not-a-date"), "—")
})

test("earliestPickup and latestDelivery pick the window bounds", () => {
  const { earliestPickup, latestDelivery } = mod
  const segments = [
    { pickupDeadline: "2026-03-10T08:00:00Z", deliveryDeadline: "2026-03-12T08:00:00Z" },
    { pickupDeadline: "2026-03-08T08:00:00Z", deliveryDeadline: "2026-03-15T08:00:00Z" },
  ]
  assert.equal(earliestPickup(segments).toISOString(), "2026-03-08T08:00:00.000Z")
  assert.equal(latestDelivery(segments).toISOString(), "2026-03-15T08:00:00.000Z")
  assert.equal(earliestPickup([]), null)
  assert.equal(latestDelivery([]), null)
})

test("hasDeadlineOrderingConflict detects overlapping sequential deadlines", () => {
  const { hasDeadlineOrderingConflict } = mod

  // Sequential, no overlap: pickup of the 2nd is after delivery of the 1st.
  assert.equal(
    hasDeadlineOrderingConflict([
      { pickupDeadline: "2026-03-01T08:00:00Z", deliveryDeadline: "2026-03-02T08:00:00Z" },
      { pickupDeadline: "2026-03-03T08:00:00Z", deliveryDeadline: "2026-03-04T08:00:00Z" },
    ]),
    false
  )

  // Overlap: the later-picked segment is due before the earlier one is delivered.
  assert.equal(
    hasDeadlineOrderingConflict([
      { pickupDeadline: "2026-03-01T08:00:00Z", deliveryDeadline: "2026-03-05T08:00:00Z" },
      { pickupDeadline: "2026-03-02T08:00:00Z", deliveryDeadline: "2026-03-03T08:00:00Z" },
    ]),
    true
  )

  assert.equal(hasDeadlineOrderingConflict([]), false)
})
