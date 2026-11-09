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

test("the city found counts as typed regardless of accents, case and punctuation", () => {
  assert.equal(locations.placeMatchesTyped("São Paulo, SP", "Av. Paulista 1000 - sao paulo/sp"), true)
  assert.equal(locations.placeMatchesTyped("São Paulo, SP", "Rua Inexistente"), false)
  // Whole words only: "Pará" is not inside "Paraná".
  assert.equal(locations.placeMatchesTyped("Pará, PA", "Rua X, Paraná"), false)
  assert.equal(locations.placeMatchesTyped(null, "Rua X"), true)
})

test("an address that landed in a city nobody typed needs confirmation until confirmed", () => {
  const lookup = { address: "Rua Inexistente", found: true, place: "São Paulo, SP", confirmed: false }
  const location = { mode: "address", address: "Rua Inexistente ", latitude: "", longitude: "", lookup }
  assert.equal(locations.needsPlaceConfirmation(location), true)
  assert.equal(locations.needsPlaceConfirmation({ ...location, lookup: { ...lookup, confirmed: true } }), false)
  // Typing again makes the lookup stale: nothing to confirm until the new text is looked up.
  assert.equal(locations.needsPlaceConfirmation({ ...location, address: "Rua Inexistente, Curitiba" }), false)
  assert.equal(locations.needsPlaceConfirmation({ ...location, lookup: { ...lookup, address: "Rua Inexistente, São Paulo", place: "São Paulo, SP" }, address: "Rua Inexistente, São Paulo" }), false)
  assert.equal(locations.needsPlaceConfirmation({ ...location, lookup: { ...lookup, found: false, place: null } }), false)
})

test("a reopened segment keeps its saved city as already confirmed", () => {
  const location = locations.locationFromText("Rua Inexistente", "São Paulo, SP")
  assert.deepEqual(location.lookup, { address: "Rua Inexistente", found: true, place: "São Paulo, SP", confirmed: true })
  assert.equal(locations.needsPlaceConfirmation(location), false)
  assert.equal(locations.locationFromText("Rua Inexistente").lookup, null)
})
