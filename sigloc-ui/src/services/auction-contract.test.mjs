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

test("carrier analysis keeps the estimated toll and vehicle volume used by the bid page", () => {
  const analysis = auctions.toCarrierAnalysis({
    competition: { activeBids: 3, bestLeaderOffer: 4700, auctionCeiling: 5000, estimatedToll: 450 },
    carrierAvailableFleet: [{ vehicleId: "v1", plate: "ABC1234", capacityWeightKg: 8000, capacityVolumeM3: 30 }],
  })
  assert.deepEqual(analysis.competition, { activeBids: 3, bestLeaderOffer: 4700, auctionCeiling: 5000, estimatedToll: 450 })
  assert.equal(analysis.carrierAvailableFleet[0].capacityVolumeM3, 30)
})

test("carrier analysis defaults a missing toll to zero", () => {
  assert.equal(auctions.toCarrierAnalysis({}).competition.estimatedToll, 0)
})
