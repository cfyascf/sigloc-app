// Cargo rules of segments and routes: one category and one temperature per segment, bulk offered
// alone, and the same locks in "Criar Rota". Plus the location field: coordinates, and the city an
// address resolved to, which the contractor confirms when it isn't the one typed.
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { tag, localDateTime } = require("../support/data")
const { createProduct, deleteProduct, createSegment, segmentCard, deleteSegment, isApi } = require("../support/flows")

test.use({ storageState: AUTH.contractor })

test.describe("cargo rules", () => {
  test.describe.configure({ mode: "serial" })

  const sku = {
    dry: tag("E2E-SECO").slice(0, 40),
    frozen: tag("E2E-CONG").slice(0, 40),
    chilled: tag("E2E-RESF").slice(0, 40),
    bulk: tag("E2E-GSOL").slice(0, 40),
  }
  const segments = []

  test.beforeAll(async ({ browser }) => {
    const page = await (await browser.newContext({ storageState: AUTH.contractor })).newPage()
    await createProduct(page, { sku: sku.dry, name: "E2E Arroz seco", weightKg: 1000 })
    await createProduct(page, { sku: sku.frozen, name: "E2E Carne congelada", environment: "Congelado (Negativo)", tempMin: -18, tempMax: -12 })
    await createProduct(page, { sku: sku.chilled, name: "E2E Iogurte resfriado", environment: "Resfriado (Positivo)", tempMin: 2, tempMax: 8 })
    await createProduct(page, { sku: sku.bulk, name: "E2E Soja a granel", category: "Granel Sólido", weightKg: 30000, volumeM3: 40 })
    await page.context().close()
  })

  test.afterAll(async ({ browser }) => {
    const page = await (await browser.newContext({ storageState: AUTH.contractor })).newPage()
    for (const segment of segments) await deleteSegment(page, segment.shortId).catch(() => {})
    for (const value of Object.values(sku)) await deleteProduct(page, value).catch(() => {})
    await page.context().close()
  })

  test("the product picker greys out what can't share the segment, and shows pt-BR weights", async ({ page }) => {
    await page.goto("/create-route-segment")
    const item = (value) => page.locator("button").filter({ hasText: value }).first()
    await expect(item(sku.dry)).toContainText("1.000 kg")
    await expect(item(sku.bulk)).toContainText("30.000 kg")

    await item(sku.dry).click()
    await expect(item(sku.frozen)).toBeDisabled()
    await expect(item(sku.frozen)).toHaveAttribute("title", /Temperatura incompatível: este produto exige -18 °C a -12 °C/)
    // 2–8 °C overlaps the 0–35 °C a dry load tolerates.
    await expect(item(sku.chilled)).toBeEnabled()
    await expect(item(sku.bulk)).toBeDisabled()
    await expect(item(sku.bulk)).toHaveAttribute("title", /Não pode ir junto com a categoria Geral/)
  })

  test("'Criar Rota' only joins segments of one category and one temperature", async ({ page }) => {
    const dry = await createSegment(page, { skus: [sku.dry], origin: "Londrina, PR", destination: "Maringá, PR" })
    segments.push(dry)
    const frozen = await createSegment(page, { skus: [sku.frozen], origin: "Curitiba, PR", destination: "Joinville, SC" })
    segments.push(frozen)
    const bulk = await createSegment(page, { skus: [sku.bulk], origin: "Cascavel, PR", destination: "Paranaguá, PR" })
    segments.push(bulk)

    await page.goto("/route-segment-management")
    await page.getByRole("button", { name: "Criar Rota" }).click()
    const box = (segment) => segmentCard(page, segment.shortId).locator("input[type=checkbox]")

    await box(dry).check()
    await expect(box(frozen)).toBeDisabled()
    await expect(box(frozen)).toHaveAttribute("title", /Uma rota precisa viajar a uma única temperatura/)
    await expect(box(bulk)).toBeDisabled()
    await expect(box(bulk)).toHaveAttribute("title", /ofertad[oa]s? sozinh/)

    await box(dry).uncheck()
    await box(bulk).check()
    await expect(box(dry)).toBeDisabled()
    await expect(box(dry)).toHaveAttribute("title", /ofertad[oa]s? sozinh/)
  })
})

test.describe("location field", () => {
  /** Pastes text into an input, as copying a pin from Google Maps would. */
  async function paste(page, selector, text) {
    await page.locator(selector).focus()
    await page.evaluate(([id, value]) => {
      const data = new DataTransfer()
      data.setData("text/plain", value)
      document.getElementById(id).dispatchEvent(new ClipboardEvent("paste", { clipboardData: data, bubbles: true, cancelable: true }))
    }, [selector.slice(1), text])
  }

  async function fillRest(page) {
    const dates = page.locator("input[type=datetime-local]")
    await dates.nth(0).fill(localDateTime(30))
    await dates.nth(1).fill(localDateTime(54))
    const money = page.locator('input[placeholder="0,00"]')
    await money.nth(0).fill("3000")
    await money.nth(1).fill("100")
  }

  /** The whole location block of a field (label, inputs, hints), by its input id. */
  const fieldBlock = (page, id) => page.locator(`#${id}`).locator('xpath=ancestor::div[contains(@class,"space-y-1.5")][1]')

  test("coordinates: a pasted pin fills both inputs, swapped values are refused, and edit reopens them", async ({ page }) => {
    await page.goto("/create-route-segment")
    await page.locator("button").filter({ hasText: env.productSku }).first().click()
    await page.getByRole("radiogroup", { name: /Local de Coleta/ }).getByRole("radio", { name: "Coordenadas" }).click()

    await paste(page, "#segment-origin-latitude", "-25,4284 -49,2733")
    await expect(page.locator("#segment-origin-latitude")).toHaveValue("-25.4284")
    await expect(page.locator("#segment-origin-longitude")).toHaveValue("-49.2733")

    // Swapped: latitude -49 is south of Brazil.
    await page.locator("#segment-origin-latitude").fill("-49.2733")
    await page.locator("#segment-origin-longitude").fill("-25.4284")
    await page.locator("#segment-destination").fill("São Paulo, SP")
    await fillRest(page)
    await page.getByRole("button", { name: "Salvar Trecho Operacional" }).click()
    await expect(page.getByText(/Latitude e longitude parecem invertidas/).first()).toBeVisible()

    await page.locator("#segment-origin-latitude").fill("-25.4284")
    await page.locator("#segment-origin-longitude").fill("-49.2733")
    const created = page.waitForResponse((r) => isApi(r, "POST", /\/api\/route-segments$/), { timeout: 60_000 })
    await page.getByRole("button", { name: "Salvar Trecho Operacional" }).click()
    const response = await created
    expect(response.status(), await response.text()).toBe(201)
    const shortId = String((await response.json()).id).slice(-8)
    try {
      // The card names the city of the point.
      await expect(segmentCard(page, shortId)).toContainText("Curitiba")
      await segmentCard(page, shortId).hover()
      await page.getByRole("button", { name: `Editar trecho ${shortId}` }).click()
      await expect(page.locator('[id$="origin-latitude"]').first()).toHaveValue("-25.4284")
      await expect(page.locator('[id$="origin-longitude"]').first()).toHaveValue("-49.2733")
    } finally {
      await deleteSegment(page, shortId)
    }
  })

  test("an address that lands in a city nobody typed must be confirmed before saving", async ({ page }) => {
    let posted = false
    page.on("request", (request) => {
      if (request.method() === "POST" && /\/api\/route-segments$/.test(new URL(request.url()).pathname)) posted = true
    })
    await page.goto("/create-route-segment")
    const origin = fieldBlock(page, "segment-origin")
    const destination = fieldBlock(page, "segment-destination")

    // A bare street name: the geocoder places it in São Paulo.
    await page.locator("#segment-origin").fill("Rua Inexistente")
    await page.locator("#segment-destination").fill("Curitiba, PR")
    await expect(origin).toContainText(/Este endereço foi localizado em .+, uma cidade que não aparece no que foi digitado/)
    await expect(destination).toContainText("Localizado em Curitiba, PR")

    await page.getByRole("button", { name: "Salvar Trecho Operacional" }).click()
    await expect(origin).toContainText("Confirme a cidade encontrada ou corrija o endereço.")
    expect(posted).toBe(false)

    await origin.getByRole("button", { name: "É este o local" }).click()
    await expect(origin).toContainText(/Localizado em .+ \(confirmado\)/)
    await expect(origin).not.toContainText("Confirme a cidade encontrada")

    // Typing the city makes it match: no confirmation needed.
    await page.locator("#segment-origin").fill("Rua XV de Novembro, Curitiba, PR")
    await expect(origin).toContainText("Localizado em Curitiba, PR")
    await expect(origin).not.toContainText("(confirmado)")

    // Gibberish is flagged right away.
    await page.locator("#segment-origin").fill("zzqqx wwvvk")
    await expect(origin).toContainText("Não encontramos este endereço.")
  })

  test("reopening a saved segment shows its city without asking again", async ({ page }) => {
    const segment = await createSegment(page, { origin: "Rua XV de Novembro, 1000, Curitiba, PR", destination: "Joinville, SC" })
    try {
      const lookups = []
      page.on("request", (request) => {
        if (/\/api\/route-segments\/locate/.test(request.url())) lookups.push(request.url())
      })
      await segmentCard(page, segment.shortId).hover()
      await page.getByRole("button", { name: `Editar trecho ${segment.shortId}` }).click()
      const origin = page.locator('input[id$="-origin"]').first()
      await expect(origin).toHaveValue("Rua XV de Novembro, 1000, Curitiba, PR")
      const id = await origin.getAttribute("id")
      await expect(fieldBlock(page, id)).toContainText("Localizado em Curitiba, PR")
      expect(lookups).toEqual([])
    } finally {
      await deleteSegment(page, segment.shortId)
    }
  })
})
