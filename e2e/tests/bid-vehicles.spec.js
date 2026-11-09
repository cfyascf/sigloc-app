// The bid form only offers vehicles the bid would accept: a body that can't carry the cargo or
// documents that block bidding grey the vehicle out, with the reason, below the usable ones.
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { randomPlate } = require("../support/data")
const { createSegment, deleteSegment, createAuction, undoAuction, registerVehicle, deleteVehicle, openBidForm } = require("../support/flows")

test.describe.configure({ mode: "serial" })

let contractor
let carrier
const run = { vehicles: [] }

test.beforeAll(async ({ browser }) => {
  contractor = await (await browser.newContext({ storageState: AUTH.contractor })).newPage()
  carrier = await (await browser.newContext({ storageState: AUTH.carrier })).newPage()
})

test.afterAll(async () => {
  if (run.auction) await undoAuction(contractor, run.auction.shortId).catch(() => {})
  if (run.segment) await deleteSegment(contractor, run.segment.shortId).catch(() => {})
  for (const plate of run.vehicles) await deleteVehicle(carrier, plate).catch(() => {})
  await contractor?.context().close()
  await carrier?.context().close()
})

test("carrier adds a tank truck and a truck whose driver's drug test expired", async () => {
  const tanker = randomPlate()
  let response = await registerVehicle(carrier, { plate: tanker, body: "Tanque", model: "E2E Tanque" })
  expect(response.status(), await response.text()).toBe(201)
  run.vehicles.push(tanker)
  run.tanker = tanker

  const expired = randomPlate()
  response = await registerVehicle(carrier, {
    plate: expired,
    driver: { name: "E2E Tox Vencido", cnh: "E", ear: true, toxInDays: -5 },
  })
  expect(response.status(), await response.text()).toBe(201)
  run.vehicles.push(expired)
  run.expired = expired
})

test("contractor auctions a dry general-cargo segment", async () => {
  run.segment = await createSegment(contractor, { destination: "Joinville, SC" })
  run.auction = await createAuction(contractor, [run.segment.shortId])
})

test("the bid form greys out both vehicles with the reason and lists them last", async () => {
  await openBidForm(carrier, run.auction.auctionId)
  const vehicle = (plate) => carrier.locator("button").filter({ hasText: plate }).first()

  await expect(vehicle(env.bidPlate)).toBeEnabled()

  await expect(vehicle(run.tanker)).toBeDisabled()
  await expect(vehicle(run.tanker)).toHaveAttribute("title", /Incompatibilidade de Equipamento: a carroceria Tanque não atende esta carga/)
  await expect(vehicle(run.tanker)).toContainText("carroceria Tanque")

  await expect(vehicle(run.expired)).toBeDisabled()
  await expect(vehicle(run.expired)).toHaveAttribute("title", /Documentação irregular: Exame toxicológico do motorista vencido em/)

  // Usable vehicles come first.
  const plates = await carrier.locator("button").filter({ hasText: /[A-Z]{3}\d[A-Z0-9]\d{2}/ }).allInnerTexts()
  const position = (plate) => plates.findIndex((text) => text.includes(plate))
  expect(position(env.bidPlate)).toBeLessThan(position(run.tanker))
  expect(position(env.bidPlate)).toBeLessThan(position(run.expired))
})
