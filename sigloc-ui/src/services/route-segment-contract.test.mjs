import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let segments

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  segments = await server.ssrLoadModule("/src/constants/route-segments.js")
})

after(async () => {
  await server?.close()
})

test("a product's cargo class: dangerous wins over fragile", () => {
  assert.equal(segments.cargoClassOfProduct({ dangerous: true, fragile: true }), "PERIGOSA")
  assert.equal(segments.cargoClassOfProduct({ fragile: true }), "FRAGIL")
  assert.equal(segments.cargoClassOfProduct({}), "COMUM")
})

test("only segments of the selected class can join a route; mixed segments never can", () => {
  assert.equal(segments.cargoSelectionBlock({ cargoClass: "COMUM" }, null), null)
  assert.equal(segments.cargoSelectionBlock({ cargoClass: "PERIGOSA" }, "PERIGOSA"), null)
  assert.match(segments.cargoSelectionBlock({ cargoClass: "FRAGIL" }, "COMUM"), /carga comum com carga frágil/)
  assert.match(segments.cargoSelectionBlock({ cargoClass: "MISTA" }, null), /mistura classes/)
})

const frozenSegment = {
  cargoClass: "COMUM",
  items: [{ transportEnvironment: "Congelado", tempMin: -18, tempMax: -12 }],
}
const drySegment = { cargoClass: "COMUM", items: [{ transportEnvironment: "Seco" }] }
const chilledSegment = {
  cargoClass: "COMUM",
  items: [{ transportEnvironment: "Resfriado", tempMin: 2, tempMax: 8 }],
}

test("a segment's temperature range is what every product tolerates; dry goods are ambient", () => {
  assert.deepEqual(segments.segmentTemperatureRange(drySegment), { min: 0, max: 35 })
  assert.deepEqual(
    segments.segmentTemperatureRange({ items: [...drySegment.items, ...chilledSegment.items] }),
    { min: 2, max: 8 }
  )
  assert.equal(segments.segmentTemperatureRange({ items: [] }), null)
})

test("only segments that can share one temperature can join the route being selected", () => {
  const frozenRange = segments.segmentTemperatureRange(frozenSegment)
  assert.equal(segments.cargoSelectionBlock(frozenSegment, "COMUM", frozenRange), null)
  assert.match(segments.cargoSelectionBlock(drySegment, "COMUM", frozenRange), /única temperatura/)

  const dryRange = segments.segmentTemperatureRange(drySegment)
  assert.equal(segments.cargoSelectionBlock(chilledSegment, "COMUM", dryRange), null)
})

test("the selection hint names the class and the temperature still allowed", () => {
  assert.equal(
    segments.selectionCompatibilityHint("FRAGIL", { min: 2, max: 8 }),
    "Para compor esta rota, selecione apenas trechos de carga frágil que possam viajar entre 2 °C e 8 °C."
  )
  assert.equal(segments.selectionCompatibilityHint(null, null), null)
})
