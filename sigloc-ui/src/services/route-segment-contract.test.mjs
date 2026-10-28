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
