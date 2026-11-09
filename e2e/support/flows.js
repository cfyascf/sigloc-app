/**
 * Business flows driven through the UI and reused across specs. Each returns the ids the API sent
 * back, read from the network response the screen itself triggered (no direct API calls).
 */
const { expect } = require("@playwright/test")
const { env } = require("./env")
const { localDateTime, isoDate, randomCpf, segmentShortId, auctionShortId } = require("./data")

const isApi = (response, method, pattern) =>
  response.request().method() === method && pattern.test(new URL(response.url()).pathname)

// ------------------------------------------------------------------ products

/**
 * Registers a catalogue product. Defaults to a dry, palletized "Geral" product; `environment` is an
 * option label ("Congelado (Negativo)") and takes `tempMin`/`tempMax`.
 */
async function createProduct(page, {
  sku,
  name,
  weightKg = 100,
  volumeM3 = 1,
  category = "Geral",
  environment = null,
  tempMin = null,
  tempMax = null,
  dangerous = false,
  fragile = false,
}) {
  await page.goto("/register-product")
  await page.getByPlaceholder("Ex: Peito de Frango Congelado").fill(name)
  await page.getByPlaceholder("Ex: FRG-001").fill(sku)
  const numbers = page.locator("input[type=number]")
  await numbers.nth(0).fill(String(weightKg))
  await numbers.nth(1).fill(String(volumeM3))
  if (category !== "Geral") await chooseFormOption(page, "Categoria", category)
  if (environment) await chooseFormOption(page, "Ambiente de Transporte", environment)
  // Number inputs: weight, volume, then the temperature range (enabled when chilled or frozen).
  if (tempMin !== null) await numbers.nth(2).fill(String(tempMin))
  if (tempMax !== null) await numbers.nth(3).fill(String(tempMax))
  if (category === "Geral") await choosePackaging(page)
  if (dangerous) await hazardToggle(page, "Carga Perigosa").click()
  if (fragile) await hazardToggle(page, "Carga Frágil").click()
  const created = page.waitForResponse((r) => isApi(r, "POST", /\/api\/products$/))
  await page.getByRole("button", { name: "Salvar Novo SKU" }).click()
  const response = await created
  expect(response.status(), await response.text()).toBe(201)
  await expect(page).toHaveURL(/\/product-management/)
  return (await response.json()).id
}

/** Packaging select of the product form ("Selecione..." until chosen). */
async function choosePackaging(page, option = "Paletizado") {
  await page.getByRole("combobox").filter({ hasText: /^Selecione/ }).click()
  await page.getByRole("option", { name: option }).click()
}

/** Radix select of the product form, found by the label above it ("Categoria", "Ambiente de Transporte"). */
function formSelect(page, label) {
  return page.locator("div.space-y-1\\.5").filter({ has: page.getByText(label, { exact: true }) }).last().getByRole("combobox")
}

async function chooseFormOption(page, label, option) {
  await formSelect(page, label).click()
  await page.getByRole("option", { name: option, exact: true }).click()
}

/** Hazard card of the product form ("Carga Perigosa" / "Carga Frágil"): a named checkbox. */
function hazardToggle(page, title) {
  return page.getByRole("checkbox", { name: title })
}

async function selectProduct(page, sku) {
  await page.goto("/product-management")
  await page.locator("button").filter({ hasText: sku }).first().click()
}

async function deleteProduct(page, sku) {
  await selectProduct(page, sku)
  await page.getByRole("button", { name: "Excluir" }).click()
  const deleted = page.waitForResponse((r) => isApi(r, "DELETE", /\/api\/products\//))
  await page.getByRole("button", { name: "Sim, Excluir" }).click()
  expect((await deleted).status()).toBe(204)
}

// ------------------------------------------------------------------ segments

/**
 * Creates a segment through "Novo Trecho". Times are hours from now. Returns { id, shortId }.
 * `skus` are catalogue products added with quantity 1.
 */
async function createSegment(page, {
  skus = [env.productSku],
  origin = "Curitiba, PR",
  destination = "São Paulo, SP",
  pickupInHours = 30,
  deliveryInHours = 54,
  ceiling = 3000,
  toll = 150,
} = {}) {
  await page.goto("/create-route-segment")
  for (const sku of skus) {
    await page.locator("button").filter({ hasText: sku }).first().click()
  }
  await page.getByPlaceholder("Ex: Curitiba, PR").fill(origin)
  await page.getByPlaceholder("Ex: São Paulo, SP").fill(destination)
  const dates = page.locator("input[type=datetime-local]")
  await dates.nth(0).fill(localDateTime(pickupInHours))
  await dates.nth(1).fill(localDateTime(deliveryInHours))
  const money = page.locator('input[placeholder="0,00"]')
  await money.nth(0).fill(String(ceiling))
  await money.nth(1).fill(String(toll))
  const created = page.waitForResponse((r) => isApi(r, "POST", /\/api\/route-segments$/), { timeout: 60_000 })
  await page.getByRole("button", { name: "Salvar Trecho Operacional" }).click()
  const response = await created
  expect(response.status(), await response.text()).toBe(201)
  await expect(page).toHaveURL(/\/route-segment-management/)
  const { id } = await response.json()
  return { id, shortId: segmentShortId(id) }
}

function segmentCard(page, shortId) {
  return page.locator("div.group").filter({ hasText: shortId }).first()
}

async function deleteSegment(page, shortId) {
  await page.goto("/route-segment-management")
  const card = segmentCard(page, shortId)
  await card.hover()
  await page.getByRole("button", { name: `Excluir trecho ${shortId}` }).click()
  const deleted = page.waitForResponse((r) => isApi(r, "DELETE", /\/api\/route-segments\//))
  await page.getByRole("button", { name: "Sim, Excluir" }).click()
  expect((await deleted).status()).toBe(204)
}

// ------------------------------------------------------------------ auctions

/** Selects the segments in "Criar Rota" and opens the route workspace. */
async function openRouteWorkspace(page, shortIds) {
  await page.goto("/route-segment-management")
  await page.getByRole("button", { name: "Criar Rota" }).click()
  for (const shortId of shortIds) {
    await segmentCard(page, shortId).locator("input[type=checkbox]").check()
  }
  await page.getByText("Avançar para Criar Leilão").click()
  await expect(page).toHaveURL(/\/offer-freight/)
}

/** Starts the auction from the workspace; returns the POST /api/auctions response. */
async function startAuction(page, expiresInHours) {
  await page.locator("#workspace-deadline").fill(localDateTime(expiresInHours))
  const created = page.waitForResponse((r) => isApi(r, "POST", /\/api\/auctions$/))
  await page.getByRole("button", { name: /Iniciar Leilão/ }).click()
  return created
}

/** Segment → auction in one go. Returns { auctionId, shortId }. */
async function createAuction(page, segmentShortIds, expiresInHours = 20) {
  await openRouteWorkspace(page, segmentShortIds)
  const response = await startAuction(page, expiresInHours)
  expect(response.status(), await response.text()).toBe(201)
  const auctionId = (await response.json()).auction.id
  return { auctionId, shortId: auctionShortId(auctionId) }
}

function auctionCard(page, shortId) {
  return page.locator("div.group").filter({ hasText: `#${shortId}` }).first()
}

/** Undoes an auction from "Fretes Ofertados" (its segments go back to available). */
async function undoAuction(page, shortId) {
  await page.goto("/freights-offered-overview")
  await auctionCard(page, shortId).hover()
  await auctionCard(page, shortId).locator("button").nth(1).click()
  const undone = page.waitForResponse((r) => isApi(r, "POST", /\/undo$/))
  await auctionCard(page, shortId).getByRole("button", { name: "Sim" }).click()
  expect((await undone).status()).toBe(204)
}

// ------------------------------------------------------------------ vehicles

const RNTRC_CATEGORY_LABEL = {
  TAC: "TAC (Transportador Autônomo)",
  ETC: "ETC (Empresa de Transporte)",
}

/**
 * Fills "Cadastrar Veículo" without saving. Defaults to a free simple truck with a valid
 * third-party RNTRC (works on any account) and no driver. `driver` takes
 * { name, cnh: "C" | "D" | "E", ear: true | false, toxInDays }; `rntrc` overrides the third-party
 * fields ({ number, category: "TAC" | "ETC", holder, document, validInDays }).
 */
async function fillVehicle(page, {
  plate,
  model = "E2E Volvo FH",
  body = "Baú",
  composition = "Caminhão simples",
  axles = "3",
  kg = "20000",
  m3 = "60",
  driver = null,
  mopp = false,
  rntrc = {},
}) {
  await page.goto("/register-vehicle")
  await page.locator("#register-plate").fill(plate)
  await page.locator("#register-model").fill(model)
  await page.locator("#register-location").fill("Curitiba, PR")
  await page.getByRole("combobox").filter({ hasText: "Selecione o implemento" }).click()
  await page.getByRole("option", { name: body, exact: true }).click()
  await page.locator("#register-composition").selectOption({ label: composition })
  await page.locator("#axle-count").fill(axles)
  // Number inputs with placeholder "0": axles, then weight and volume.
  const capacity = page.locator('input[type=number][placeholder="0"]')
  await capacity.nth(1).fill(kg)
  await capacity.nth(2).fill(m3)
  if (mopp) await page.getByRole("checkbox", { name: /Motorista com MOPP/ }).check()
  if (driver) {
    await page.locator("#register-driver").fill(driver.name)
    if (driver.cnh) await page.locator("#register-cnh").selectOption({ label: driver.cnh })
    if (driver.ear !== undefined) await page.locator("#register-ear").selectOption(driver.ear ? "yes" : "no")
    if (driver.toxInDays !== undefined) await page.locator("#register-tox").fill(isoDate(driver.toxInDays))
  }
  const third = { number: "12345678", category: "TAC", holder: "E2E Autônomo", document: randomCpf(), validInDays: 300, ...rntrc }
  await page.getByRole("radio", { name: "De terceiro (agregado)" }).click()
  await page.locator("#register-rntrc-number").fill(third.number)
  await page.locator("#register-rntrc-category").selectOption({ label: RNTRC_CATEGORY_LABEL[third.category] })
  await page.locator("#register-rntrc-holder").fill(third.holder)
  await page.locator("#register-rntrc-document").fill(third.document)
  await page.locator("#register-rntrc-valid").fill(isoDate(third.validInDays))
}

/** Fills and saves a vehicle; returns the POST /api/vehicles response. */
async function registerVehicle(page, vehicle) {
  await fillVehicle(page, vehicle)
  const sent = page.waitForResponse((r) => isApi(r, "POST", /\/api\/vehicles$/))
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  return sent
}

/** Fleet row of a plate. */
function fleetRow(page, plate) {
  return page.getByText(plate, { exact: true }).first().locator("xpath=ancestor::div[.//button][1]")
}

async function deleteVehicle(page, plate) {
  await page.goto("/fleet-management")
  await page.getByRole("button", { name: `Excluir veículo ${plate}` }).click()
  const deleted = page.waitForResponse((r) => isApi(r, "DELETE", /\/api\/vehicles\//))
  await page.getByRole("button", { name: "Sim, Excluir" }).click()
  expect((await deleted).status()).toBe(204)
}

// ------------------------------------------------------------------ bids and award

async function openBidForm(page, auctionId) {
  await page.goto(`/bid-analysis/${auctionId}/place-bid`)
  await expect(page.getByText("Valor do Frete")).toBeVisible()
}

/** Places or updates the carrier's bid; returns the POST /bids response. */
async function submitBid(page, auctionId, { plate = env.bidPlate, freight }) {
  await openBidForm(page, auctionId)
  await page.locator("button").filter({ hasText: plate }).first().click()
  await page.locator("input[type=number]").first().fill(String(freight))
  const sent = page.waitForResponse((r) => isApi(r, "POST", /\/bids$/))
  await page.getByRole("button", { name: /Confirmar Lance|Atualizar Lance/ }).last().click()
  return sent
}

/** Contractor awards the best bid in "Análise de Lances"; returns the trip id. */
async function awardBestBid(page, auctionId) {
  await page.goto(`/auction-bids/${auctionId}`)
  await page.getByRole("button", { name: /Adjudicar/ }).first().click()
  const awarded = page.waitForResponse((r) => isApi(r, "POST", /\/award$/))
  await page.getByRole("button", { name: "Confirmar adjudicação" }).click()
  const response = await awarded
  expect(response.status(), await response.text()).toBe(201)
  return (await response.json()).tripId
}

module.exports = {
  isApi,
  choosePackaging,
  chooseFormOption,
  formSelect,
  hazardToggle,
  createProduct,
  selectProduct,
  deleteProduct,
  createSegment,
  segmentCard,
  deleteSegment,
  openRouteWorkspace,
  startAuction,
  createAuction,
  auctionCard,
  undoAuction,
  fillVehicle,
  registerVehicle,
  fleetRow,
  deleteVehicle,
  openBidForm,
  submitBid,
  awardBestBid,
}
