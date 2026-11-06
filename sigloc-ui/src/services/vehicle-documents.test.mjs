import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let vehicles
let validation

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  vehicles = await server.ssrLoadModule("/src/services/vehicle-service.js")
  validation = await server.ssrLoadModule("/src/lib/vehicle-validation.js")
})

after(async () => {
  await server?.close()
})

const thirdParty = {
  composition: 2,
  rntrcSource: 2,
  rntrcNumber: " 012345678 ",
  rntrcCategory: 1,
  rntrcHolderName: " João da Silva ",
  rntrcHolderDocument: "529.982.247-25",
  rntrcValidUntil: "2027-06-30",
  driverCnhCategory: 3,
  driverHasEar: true,
  driverToxicologicalExamValidUntil: "2027-01-31",
  hasMopp: false,
  moppValidUntil: "2027-01-01",
  civCippValidUntil: "",
}

test("documents are sent with enums as numbers, digits-only documents and blanks as null", () => {
  assert.deepEqual(vehicles.toDocumentsDto(thirdParty), {
    composition: 2,
    rntrcSource: 2,
    rntrcNumber: "012345678",
    rntrcCategory: 1,
    rntrcHolderName: "João da Silva",
    rntrcHolderDocument: "52998224725",
    rntrcValidUntil: "2027-06-30",
    driverCnhCategory: 3,
    driverHasEar: true,
    driverToxicologicalExamValidUntil: "2027-01-31",
    moppValidUntil: null, // no MOPP, so no validity
    civCippValidUntil: null,
  })
})

test("an older vehicle without documents opens with empty fields and EAR unknown", () => {
  const form = vehicles.documentsToForm(null)
  assert.equal(form.composition, "")
  assert.equal(form.rntrcSource, "")
  assert.equal(form.driverHasEar, null)
  assert.equal(vehicles.toDocumentsDto(form).driverHasEar, null)
})

test("a new vehicle needs its documents; the driver's only when a driver is assigned", () => {
  const base = { plate: "ABC1D23", model: "Volvo", axleCount: 3, weightKg: 1000, volumeM3: 10, bodyType: "Baú" }
  const missing = validation.validateVehicle({ ...base, ...vehicles.documentsToForm(null) }, { creating: true })
  assert.ok(missing.composition && missing.rntrcSource)
  assert.equal(missing.driverCnhCategory, undefined)

  const withDriver = validation.validateVehicle({ ...base, ...vehicles.documentsToForm(null), driver: "João" }, { creating: true })
  assert.ok(withDriver.driverCnhCategory && withDriver.driverHasEar && withDriver.driverToxicologicalExamValidUntil)

  assert.deepEqual(validation.validateVehicle({ ...base, ...thirdParty, driver: "João" }, { creating: true }), {})
  // On edit, missing documents stay optional.
  assert.deepEqual(validation.validateVehicle({ ...base, ...vehicles.documentsToForm(null) }), {})
})
