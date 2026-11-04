import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let locations

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  locations = await server.ssrLoadModule("/src/lib/locations.js")
})

after(async () => {
  await server?.close()
})

test("a pasted pair is split latitude first, with dot or comma decimals", () => {
  const expected = { latitude: "-25.4284", longitude: "-49.2733" }
  assert.deepEqual(locations.parseCoordinatePair("-25.4284, -49.2733"), expected)
  assert.deepEqual(locations.parseCoordinatePair(" -25.4284;-49.2733 "), expected)
  assert.deepEqual(locations.parseCoordinatePair("-25,4284 -49,2733"), expected)
  assert.equal(locations.parseCoordinatePair("Curitiba, PR"), null)
  assert.equal(locations.parseCoordinatePair("-25.4284"), null)
})

test("a stored coordinate reopens in coordinates mode; any other text is an address", () => {
  assert.deepEqual(locations.locationFromText("-25.428400, -49.273300"), {
    mode: "coordinates", address: "", latitude: "-25.4284", longitude: "-49.2733",
  })
  assert.equal(locations.locationFromText("Curitiba, PR").mode, "address")
  assert.equal(locations.locationFromText("Curitiba, PR").address, "Curitiba, PR")
})

test("each end is sent as an address or as a coordinate, never both", () => {
  assert.deepEqual(locations.toLocationRequest({ mode: "address", address: " Curitiba, PR ", latitude: "1", longitude: "2" }), {
    text: "Curitiba, PR", coordinate: null,
  })
  assert.deepEqual(locations.toLocationRequest({ mode: "coordinates", address: "x", latitude: "-25,4284", longitude: "-49.2733" }), {
    text: null, coordinate: { latitude: -25.4284, longitude: -49.2733 },
  })
  assert.deepEqual(locations.toLocationRequest({ mode: "coordinates", address: "", latitude: "", longitude: "-49" }).coordinate, {
    latitude: null, longitude: -49,
  })
  assert.deepEqual(locations.toLocationRequest("São Paulo, SP"), { text: "São Paulo, SP", coordinate: null })
})
