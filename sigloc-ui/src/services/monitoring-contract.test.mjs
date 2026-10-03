import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let trips
let vehicles
let client
const originalFetch = globalThis.fetch

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  trips = await server.ssrLoadModule("/src/services/trip-service.js")
  vehicles = await server.ssrLoadModule("/src/services/vehicle-service.js")
  client = await server.ssrLoadModule("/src/lib/api-client.js")
  client.setAuthTokenProvider(() => "test-token")
})

after(async () => {
  globalThis.fetch = originalFetch
  await server?.close()
})

function respond(body, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } })
}

test("active list defaults to 20, caps at 100, sends filters and never fetches detail", async () => {
  const calls = []
  globalThis.fetch = async (url, options) => {
    calls.push({ url: new URL(url), options })
    return respond({ viagens: [], totalItems: 0, totalPages: 0, page: 1, pageSize: 20 })
  }
  await trips.listActiveTrips()
  await trips.listActiveTrips({ search: "  Placa  ", status: "ATRASADO", risk: "CRITIC", page: 2, pageSize: 200 })
  assert.equal(calls.length, 2)
  assert.equal(calls[0].url.pathname, "/api/viagens/ativas")
  assert.equal(calls[0].url.searchParams.get("pageSize"), "20")
  assert.equal(calls[1].url.searchParams.get("pageSize"), "100")
  assert.equal(calls[1].url.searchParams.get("search"), "Placa")
  assert.equal(calls[1].url.searchParams.get("status"), "ATRASADO")
  assert.equal(calls[1].url.searchParams.get("risk"), "CRITIC")
  assert.equal(calls[1].options.headers.Authorization, "Bearer test-token")
})

test("detail uses trip identity, propagates cancellation, and preserves stale/cache metadata", async () => {
  const controller = new AbortController()
  const calls = []
  globalThis.fetch = async (url, options) => {
    calls.push({ url: new URL(url), options })
    return respond({ id: "trip-id", rotaId: "route-id", isCacheRenovado: false, cacheDesatualizado: true, aviso: "GPS indisponível", ultimaPosicaoEm: "2026-10-01T12:00:00Z", ultimaAtualizacao: "2026-10-01T12:05:00Z" })
  }
  const result = await trips.getTripDetails("trip-id", { signal: controller.signal })
  assert.equal(calls[0].url.pathname, "/api/viagens/trip-id/detalhes")
  assert.equal(calls[0].url.search, "")
  assert.equal(calls[0].options.method, "GET")
  assert.equal(calls[0].options.signal, controller.signal)
  assert.equal(result.stale, true)
  assert.equal(result.cacheRenewed, false)
  assert.equal(result.warning, "GPS indisponível")
  assert.notEqual(result.lastPing, result.lastCalculation)
  assert.equal(result.progress, null)
  assert.equal(result.latitude, null)
})

test("detail preserves supplied timeline order, colors, actions and terminal lifecycle", () => {
  const result = trips.toTripDetails({ id: "trip", status: "ENTREGUE", progressoPercentual: 100, ocupacaoPercentual: 125, paradas: [
    { id: "visit-2", cidade: "Curitiba", estado: "PR", cor: "VERDE", status: "CONCLUIDA", acoes: [{ id: "action-1", tipo: "ENTREGA", concluida: true, fonteConclusao: "GEOFENCE_INFERRED", prazo: "2026-10-01T16:00:00Z", concluidaEm: "2026-10-01T15:00:00Z" }] },
    { id: "visit-1", cidade: "Curitiba", estado: "PR", cor: "CINZA", status: "PENDENTE", acoes: [] },
  ], eventos: [{ id: "event", ocorridoEm: "2026-10-01T15:00:00Z", descricao: "Parada concluída por geofence" }] })
  assert.equal(result.lifecycle, "ENTREGUE")
  assert.equal(result.utilization, 125)
  assert.deepEqual(result.timeline.map((stop) => stop.id), ["visit-2", "visit-1"])
  assert.equal(result.timeline[0].color, "VERDE")
  assert.equal(result.timeline[0].actions[0].source, "GEOFENCE_INFERRED")
  assert.equal(result.timeline[0].actions[0].completed, true)
  assert.equal(result.events[0].message, "Parada concluída por geofence")
})

test("503 without a snapshot remains an API error and malformed list is not an empty success", async () => {
  globalThis.fetch = async () => respond({ error: "TRACKING_UNAVAILABLE", message: "Indisponível" }, 503)
  await assert.rejects(trips.getTripDetails("trip"), { status: 503, code: "TRACKING_UNAVAILABLE" })
  globalThis.fetch = async () => respond({})
  await assert.rejects(trips.listActiveTrips(), { code: "INVALID_RESPONSE" })
})

test("vehicle tracking/contact fields round-trip, trim and clear, rejecting invalid tracker IDs", () => {
  const vehicle = vehicles.toVehicle({ id: "vehicle", traccarDeviceId: 42, driverPhone: " +55 41 1234 ", bodyType: 1, status: 0 })
  const update = vehicles.toUpdateDto(vehicle)
  assert.equal(update.traccarDeviceId, 42)
  assert.equal(update.driverPhone, "+55 41 1234")
  assert.equal("plate" in update, false)
  assert.equal(vehicles.toUpdateDto({ ...vehicle, traccarDeviceId: "", driverPhone: "" }).traccarDeviceId, null)
  assert.equal(vehicles.toUpdateDto({ ...vehicle, traccarDeviceId: "", driverPhone: "" }).driverPhone, "")
  assert.equal(vehicles.toUpdateDto({ ...vehicle, driverPhone: undefined }).driverPhone, null)
  assert.equal(vehicles.toCreateDto({ ...vehicle, driverPhone: "" }).driverPhone, null)
  for (const value of [0, -1, 1.5, "abc", Number.MAX_SAFE_INTEGER + 1]) {
    assert.throws(() => vehicles.toUpdateDto({ ...vehicle, traccarDeviceId: value }), { code: "VALIDATION_ERROR" })
  }
  const create = vehicles.toCreateDto({ ...vehicle, plate: "abc-1234", axleCount: 6 })
  assert.equal(create.plate, "ABC1234")
  assert.equal(create.traccarDeviceId, 42)
  assert.equal("status" in create, false)
})
