import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let auctions

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  auctions = await server.ssrLoadModule("/src/services/auction-service.js")
})

after(async () => {
  await server?.close()
})

test("carrier analysis keeps the estimated toll, ANTT floor and vehicle volume used by the bid page", () => {
  const analysis = auctions.toCarrierAnalysis({
    competition: { activeBids: 3, bestLeaderOffer: 4700, auctionCeiling: 5000, estimatedToll: 450, estimatedAnttFloor: 4000 },
    carrierAvailableFleet: [{ vehicleId: "v1", plate: "ABC1234", capacityWeightKg: 8000, capacityVolumeM3: 30 }],
  })
  assert.deepEqual(analysis.competition, { activeBids: 3, bestLeaderOffer: 4700, auctionCeiling: 5000, estimatedToll: 450, estimatedAnttFloor: 4000 })
  assert.equal(analysis.carrierAvailableFleet[0].capacityVolumeM3, 30)
})

test("carrier analysis defaults a missing toll to zero and a missing floor to null", () => {
  assert.equal(auctions.toCarrierAnalysis({}).competition.estimatedToll, 0)
  assert.equal(auctions.toCarrierAnalysis({}).competition.estimatedAnttFloor, null)
})

test("bid-form vehicles keep the reason the bid would refuse them", () => {
  const analysis = auctions.toCarrierAnalysis({
    carrierAvailableFleet: [
      { vehicleId: "v1", plate: "TAN1A02", unavailableReason: "Incompatibilidade de Equipamento: a carroceria Tanque não atende esta carga." },
      { vehicleId: "v2", plate: "ABC1D23" },
    ],
  })
  assert.match(analysis.carrierAvailableFleet[0].unavailableReason, /carroceria Tanque/)
  assert.equal(analysis.carrierAvailableFleet[1].unavailableReason, null)
})
