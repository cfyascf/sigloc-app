// Route segments: validation, create/edit/delete, and the cargo-class rule (dangerous, fragile and
// common goods never share a segment or a route).
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { tag, localDateTime } = require("../support/data")
const { createProduct, deleteProduct, createSegment, segmentCard, deleteSegment, isApi } = require("../support/flows")

test.use({ storageState: AUTH.contractor })

/** Fills "Novo Trecho" and submits; returns the API status. */
async function submitSegment(page, { skus = [env.productSku], origin = "Curitiba, PR", destination = "Londrina, PR", pickup = 30, delivery = 54, ceiling = "2000", toll = "0" }) {
  await page.goto("/create-route-segment")
  for (const sku of skus) await page.locator("button").filter({ hasText: sku }).first().click()
  await page.getByPlaceholder("Ex: Curitiba, PR").fill(origin)
  await page.getByPlaceholder("Ex: São Paulo, SP").fill(destination)
  const dates = page.locator("input[type=datetime-local]")
  await dates.nth(0).fill(localDateTime(pickup))
  await dates.nth(1).fill(localDateTime(delivery))
  const money = page.locator('input[placeholder="0,00"]')
  await money.nth(0).fill(ceiling)
  await money.nth(1).fill(toll)
  const sent = page.waitForResponse((r) => isApi(r, "POST", /\/api\/route-segments$/), { timeout: 60_000 })
  await page.getByRole("button", { name: "Salvar Trecho Operacional" }).click()
  return (await sent).status()
}

test.describe("validation", () => {
  test("an empty segment shows every required field", async ({ page }) => {
    await page.goto("/create-route-segment")
    await page.getByRole("button", { name: "Salvar Trecho Operacional" }).click()
    await expect(page.getByText("O trecho deve conter ao menos um produto.").first()).toBeVisible()
    await expect(page.getByText("Campo obrigatório.").first()).toBeVisible()
    await expect(page).toHaveURL(/\/create-route-segment/)
  })

  const cases = [
    ["delivery before pickup", { pickup: 54, delivery: 30 }, "A entrega deve ser posterior à coleta."],
    ["pickup in the past", { pickup: -24, delivery: 30 }, "O prazo de coleta deve estar no futuro."],
    ["same origin and destination", { destination: "Curitiba, PR" }, "O destino deve ser diferente da origem."],
    ["negative money", { ceiling: "-100", toll: "-5" }, "Deve ser zero ou maior."],
    ["toll not below the ceiling", { ceiling: "100", toll: "500" }, "O pedágio estimado deve ser menor que o orçamento teto."],
  ]
  for (const [name, input, message] of cases) {
    test(`rejects ${name}`, async ({ page }) => {
      expect(await submitSegment(page, input)).toBe(400)
      await expect(page.getByText(message).first()).toBeVisible()
      await expect(page).toHaveURL(/\/create-route-segment/)
    })
  }

  // Pure gibberish: Pelias finds nothing. (Words like "inexistente" can match a real street,
  // e.g. "Rua Inexistente" in São Paulo, so they don't make a reliable negative case.)
  test("rejects an address that does not exist instead of guessing a city", async ({ page }) => {
    expect(await submitSegment(page, { origin: "zzqqx wwvvk" })).not.toBe(201)
    await expect(page.getByText(/Endereço não encontrado/).first()).toBeVisible()
    await expect(page).toHaveURL(/\/create-route-segment/)
  })
})

test("create a segment with a full address, edit it and delete it", async ({ page }) => {
  const segment = await createSegment(page, {
    origin: "Rua XV de Novembro, 1000, Curitiba, PR",
    destination: "Avenida Paulista, 1578, São Paulo, SP",
    ceiling: 3000,
    toll: 150,
  })
  const card = segmentCard(page, segment.shortId)
  // The card shows the geocoded cities, not the street.
  await expect(card).toContainText("Curitiba")
  await expect(card).toContainText("São Paulo")
  await expect(card).not.toContainText("Rua XV")
  await expect(card).toContainText("R$ 3.000")

  await card.hover()
  await page.getByRole("button", { name: `Editar trecho ${segment.shortId}` }).click()
  // The edit form has number inputs for ceiling, toll and quantity; change the one holding 3000.
  const numbers = page.locator("input[type=number]")
  for (let i = 0; i < (await numbers.count()); i++) {
    if ((await numbers.nth(i).inputValue()) === "3000") {
      await numbers.nth(i).fill("3100")
      break
    }
  }
  const saved = page.waitForResponse((r) => isApi(r, "PUT", /\/api\/route-segments\//), { timeout: 60_000 })
  await page.getByRole("button", { name: /Salvar/ }).first().click()
  expect((await saved).status()).toBe(200)
  await page.reload()
  await expect(segmentCard(page, segment.shortId)).toContainText("R$ 3.100")

  // Delivery before pickup is refused on edit too, and nothing changes.
  await segmentCard(page, segment.shortId).hover()
  await page.getByRole("button", { name: `Editar trecho ${segment.shortId}` }).click()
  const dates = page.locator("input[type=datetime-local]")
  await dates.nth(0).fill(localDateTime(54))
  await dates.nth(1).fill(localDateTime(30))
  await page.getByRole("button", { name: /Salvar/ }).first().click()
  await expect(page.getByText("A entrega deve ser posterior à coleta.").first()).toBeVisible()

  await deleteSegment(page, segment.shortId)
  await page.reload()
  await expect(segmentCard(page, segment.shortId)).toHaveCount(0)
})

test.describe("cargo classes", () => {
  test("a segment keeps a single class and a route only joins segments of the same class", async ({ page }) => {
    const dangerousSku = tag("E2E-HZ").slice(0, 40)
    await createProduct(page, { sku: dangerousSku, name: "E2E Produto Perigoso", dangerous: true })
    const created = []
    try {
      // In "Novo Trecho", once a common product is in, the dangerous one is locked out.
      await page.goto("/create-route-segment")
      await page.locator("button").filter({ hasText: env.productSku }).first().click()
      const dangerousItem = page.locator("button").filter({ hasText: dangerousSku }).first()
      await expect(dangerousItem).toBeDisabled()
      await expect(dangerousItem).toContainText("Não pode ir junto com carga comum")

      // One segment of each class.
      created.push(await createSegment(page, { destination: "Joinville, SC" }))
      created.push(await createSegment(page, { skus: [dangerousSku], destination: "Ponta Grossa, PR" }))
      const [common, dangerous] = created
      // Marked with a hazard icon whose title explains the class.
      await expect(segmentCard(page, dangerous.shortId).locator('[title^="Perigosa"]').first()).toBeAttached()

      // "Criar Rota": after ticking the common segment, the dangerous one can't be ticked.
      await page.goto("/route-segment-management")
      await page.getByRole("button", { name: "Criar Rota" }).click()
      await segmentCard(page, common.shortId).locator("input[type=checkbox]").check()
      const blocked = segmentCard(page, dangerous.shortId).locator("input[type=checkbox]")
      await expect(blocked).toBeDisabled()
      await expect(blocked).toHaveAttribute("title", /não pode misturar carga comum com carga perigosa/)
    } finally {
      for (const segment of created) await deleteSegment(page, segment.shortId)
      await deleteProduct(page, dangerousSku)
    }
  })
})
