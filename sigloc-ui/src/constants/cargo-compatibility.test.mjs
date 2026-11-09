import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let rules
let segments

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  rules = await server.ssrLoadModule("/src/constants/cargo-compatibility.js")
  segments = await server.ssrLoadModule("/src/constants/route-segments.js")
})

after(async () => {
  await server?.close()
})

test("each category is carried by its own body types (same table as the API)", () => {
  assert.deepEqual(rules.acceptedBodies("GranelSolido"), ["Graneleiro", "Caçamba"])
  assert.deepEqual(rules.acceptedBodies("GranelLiquido"), ["Tanque"])
  assert.deepEqual(rules.acceptedBodies("CargaViva"), ["Gaiola / Boiadeiro"])
  assert.deepEqual(rules.acceptedBodies("Neogranel"), ["Grade Baixa", "Prancha", "Cegonheiro"])
})

test("refrigeration and fragility narrow the accepted bodies", () => {
  assert.deepEqual(rules.acceptedBodies("Geral", "Congelado"), ["Baú Refrigerado"])
  assert.deepEqual(rules.acceptedBodies("Geral", "Seco", true), ["Baú", "Baú Refrigerado", "Sider"])
  assert.deepEqual(rules.acceptedBodies("GranelLiquido", "Congelado"), [])
})

test("a category only offers the environments its bodies reach", () => {
  assert.deepEqual(rules.environmentsFor("Geral"), ["Seco", "Resfriado", "Congelado"])
  assert.deepEqual(rules.environmentsFor("GranelLiquido"), ["Seco", "Resfriado"])
  assert.deepEqual(rules.environmentsFor("CargaViva"), ["Seco"])
})

test("switching a product's category drops what the new category can't have", () => {
  const form = rules.normalizeProductForm({ category: "CargaViva", transportEnvironment: "Congelado", dangerous: true, fragile: true })
  assert.equal(form.transportEnvironment, "Seco")
  assert.equal(form.dangerous, false)
  assert.equal(form.fragile, false)
})

test("a body type fixes the refrigeration and cargo securing the vehicle can declare", () => {
  assert.deepEqual(rules.normalizeVehicleEquipment({ bodyType: "Sider", refrigerationLevel: 2 }).refrigerationLevel, 0)
  assert.equal(rules.normalizeVehicleEquipment({ bodyType: "Baú Refrigerado", refrigerationLevel: 0 }).refrigerationLevel, 1)
  assert.equal(rules.normalizeVehicleEquipment({ bodyType: "Tanque", refrigerationLevel: 2 }).refrigerationLevel, 1)
  assert.equal(rules.normalizeVehicleEquipment({ bodyType: "Prancha", hasCargoSecuring: false }).hasCargoSecuring, true)
  assert.deepEqual(rules.categoriesFor("Graneleiro"), ["Geral", "Granel Sólido"])
})

test("a route only joins segments of one product category", () => {
  const general = { cargoClass: "COMUM", items: [{ category: "Geral" }] }
  const vehicles = { cargoClass: "COMUM", items: [{ category: "Neogranel" }] }
  const mixed = { cargoClass: "COMUM", items: [{ category: "Geral" }, { category: "CargaViva" }] }
  assert.equal(segments.cargoSelectionBlock(general, "COMUM", null, "Geral"), null)
  assert.match(segments.cargoSelectionBlock(vehicles, "COMUM", null, "Geral"), /categorias Geral e Neogranel/)
  assert.match(segments.cargoSelectionBlock(mixed, null), /mistura categorias/)
})

test("bulk and live cargo segments are offered alone and carry a single product", () => {
  const milkA = { cargoClass: "COMUM", items: [{ productId: "milk", category: "GranelLiquido" }] }
  const general = { cargoClass: "COMUM", items: [{ productId: "box", category: "Geral" }] }
  const twoGrains = { cargoClass: "COMUM", items: [{ productId: "soy", category: "GranelSolido" }, { productId: "corn", category: "GranelSolido" }] }
  assert.equal(rules.isSingleProductCategory("GranelLiquido"), true)
  assert.equal(rules.isSingleProductCategory("Geral"), false)
  assert.match(segments.cargoSelectionBlock(milkA, "COMUM", null, "GranelLiquido"), /ofertadas sozinhas/)
  assert.match(segments.cargoSelectionBlock(milkA, "COMUM", null, "Geral"), /ofertado sozinho/)
  assert.equal(segments.cargoSelectionBlock(milkA, null), null)
  assert.match(segments.cargoSelectionBlock(twoGrains, null), /mais de um produto/)
  assert.equal(segments.cargoSelectionBlock(general, "COMUM", null, "Geral"), null)
  assert.match(segments.selectionCompatibilityHint("COMUM", null, "CargaViva"), /ofertados sozinhos/)
})

test("effective refrigeration is capped by what the body allows", () => {
  // Levels: 0 nenhuma, 1 resfriado, 2 congelado.
  assert.equal(rules.effectiveRefrigeration("Grade Baixa", 1), 0)
  assert.equal(rules.effectiveRefrigeration("Tanque", 2), 1)
  assert.equal(rules.effectiveRefrigeration("Baú Refrigerado", 2), 2)
  assert.equal(rules.effectiveRefrigeration("Baú", undefined), 0)
})
