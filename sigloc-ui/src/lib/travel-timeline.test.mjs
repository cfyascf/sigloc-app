import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let timeline

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  timeline = await server.ssrLoadModule("/src/lib/travel-timeline.js")
})

after(async () => {
  await server?.close()
})

test("different cities keep one node per city titled by the city", () => {
  const { buildUnifiedTimeline } = timeline
  const stops = buildUnifiedTimeline([
    {
      id: "0000000000000000000000000000b4498f37",
      origin: "Rua A, Curitiba, PR",
      destination: "Rua B, São Paulo, SP",
      originCity: "Curitiba, PR",
      destinationCity: "São Paulo, SP",
      items: [{ name: "Lombo Suíno" }],
    },
  ])

  assert.equal(stops.length, 2)
  assert.equal(stops[0].city, "Curitiba")
  assert.equal(stops[1].city, "São Paulo")
})

test("stops carry their own deadline as the time pill", () => {
  const { buildUnifiedTimeline } = timeline
  const stops = buildUnifiedTimeline([
    {
      id: "0000000000000000000000000000b4498f37",
      origin: "Rua A, Curitiba, PR",
      destination: "Rua B, São Paulo, SP",
      originCity: "Curitiba, PR",
      destinationCity: "São Paulo, SP",
      pickupDeadline: "2026-03-05T14:30:00-03:00",
      deliveryDeadline: "2026-03-07T09:00:00-03:00",
      items: [{ name: "Lombo Suíno" }],
    },
  ])

  assert.equal(stops[0].time, "05/03/2026 14:30")
  assert.equal(stops[1].time, "07/03/2026 09:00")
})

test("same city splits into two nodes titled by neighbourhood", () => {
  const { buildUnifiedTimeline } = timeline
  const stops = buildUnifiedTimeline([
    {
      id: "00000000000000000000000000014e347d3",
      origin: "Rua A, 100, Centro, Curitiba, PR",
      destination: "Rua B, 200, Batel, Curitiba, PR",
      originCity: "Curitiba, PR",
      destinationCity: "Curitiba, PR",
      items: [{ name: "Peito de Frango" }],
    },
  ])

  assert.equal(stops.length, 2)
  assert.equal(stops[0].city, "Centro")
  assert.equal(stops[1].city, "Batel")
  assert.match(stops[0].actions[0], /^Coleta ·/)
  assert.match(stops[1].actions[0], /^Descarga ·/)
})

test("same city still shows two nodes when no neighbourhood is given", () => {
  const { buildUnifiedTimeline } = timeline
  const stops = buildUnifiedTimeline([
    {
      id: "00000000000000000000000000000abcdef0",
      origin: "Curitiba, PR",
      destination: "Curitiba, PR",
      originCity: "Curitiba, PR",
      destinationCity: "Curitiba, PR",
      items: [{ name: "Carga" }],
    },
  ])

  assert.equal(stops.length, 2)
  assert.equal(stops[0].city, "Curitiba")
  assert.equal(stops[1].city, "Curitiba")
  assert.match(stops[0].actions[0], /^Coleta ·/)
  assert.match(stops[1].actions[0], /^Descarga ·/)
})

test("shared city across segments still consolidates into one node", () => {
  const { buildUnifiedTimeline } = timeline
  const stops = buildUnifiedTimeline([
    {
      id: "00000000000000000000000000000000aaaa",
      origin: "Rua A, Curitiba, PR",
      destination: "Rua B, São Paulo, SP",
      originCity: "Curitiba, PR",
      destinationCity: "São Paulo, SP",
      items: [{ name: "Carga 1" }],
    },
    {
      id: "00000000000000000000000000000000bbbb",
      origin: "Rua C, São Paulo, SP",
      destination: "Rua D, Salvador, BA",
      originCity: "São Paulo, SP",
      destinationCity: "Salvador, BA",
      items: [{ name: "Carga 2" }],
    },
  ])

  assert.deepEqual(
    stops.map((s) => s.city),
    ["Curitiba", "São Paulo", "Salvador"]
  )
  // São Paulo is a drop-off for segment 1 and a pickup for segment 2.
  assert.equal(stops[1].actions.length, 2)
})
