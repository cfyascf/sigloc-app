// Product catalogue: create, validate, edit, cancel and delete a product of this run.
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { tag } = require("../support/data")
const { createProduct, selectProduct, deleteProduct, isApi, choosePackaging, chooseFormOption, formSelect, hazardToggle } = require("../support/flows")

test.use({ storageState: AUTH.contractor })

test("an empty product form shows field errors", async ({ page }) => {
  await page.goto("/register-product")
  await page.getByRole("button", { name: "Salvar Novo SKU" }).click()
  await expect(page.getByText("Obrigatório.").first()).toBeVisible()
  await expect(page.getByText("Deve ser maior que zero.").first()).toBeVisible()
  await expect(page).toHaveURL(/\/register-product/)
})

test("a SKU that only differs in case is a duplicate", async ({ page }) => {
  await page.goto("/register-product")
  await page.getByPlaceholder("Ex: Peito de Frango Congelado").fill("E2E duplicado")
  await page.getByPlaceholder("Ex: FRG-001").fill(env.productSku.toLowerCase())
  const numbers = page.locator("input[type=number]")
  await numbers.nth(0).fill("10")
  await numbers.nth(1).fill("1")
  await choosePackaging(page)
  const sent = page.waitForResponse((r) => isApi(r, "POST", /\/api\/products$/))
  await page.getByRole("button", { name: "Salvar Novo SKU" }).click()
  expect((await sent).status()).toBe(409)
  await expect(page.getByText(/Já existe um produto com o SKU/).first()).toBeVisible()
})

test("create, edit, cancel an edit, and delete a product", async ({ page }) => {
  const sku = tag("E2E-P").slice(0, 40)
  await createProduct(page, { sku, name: "E2E Produto <b>teste</b> & \"ç\"", weightKg: 25.5, volumeM3: 0.75 })

  // Shown as typed (no HTML), with a pt-BR weight.
  await page.goto("/product-management")
  const item = page.locator("button").filter({ hasText: sku }).first()
  await expect(item).toContainText('E2E Produto <b>teste</b> & "ç"')
  await expect(item).toContainText("25,5 kg")

  // Edit persists after a reload.
  await selectProduct(page, sku)
  await page.getByRole("button", { name: "Editar" }).click()
  await page.getByPlaceholder("Ex: Peito de Frango Congelado").fill("E2E Produto editado")
  await page.getByRole("button", { name: /Salvar/ }).first().click()
  await page.reload()
  await selectProduct(page, sku)
  await expect(page.getByPlaceholder("Ex: Peito de Frango Congelado")).toHaveValue("E2E Produto editado")

  // Cancelling an edit discards it.
  await page.getByRole("button", { name: "Editar" }).click()
  await page.getByPlaceholder("Ex: Peito de Frango Congelado").fill("NAO DEVE PERSISTIR")
  await page.getByRole("button", { name: /Cancelar/ }).first().click()
  await page.reload()
  await expect(page.locator("body")).not.toContainText("NAO DEVE PERSISTIR")

  await deleteProduct(page, sku)
  await page.reload()
  await expect(page.locator("button").filter({ hasText: sku })).toHaveCount(0)
})

test("the category decides the environments and the hazard flags, which are named checkboxes", async ({ page }) => {
  await page.goto("/register-product")
  const dangerous = hazardToggle(page, "Carga Perigosa")
  const fragile = hazardToggle(page, "Carga Frágil")
  const environments = async () => {
    await formSelect(page, "Ambiente de Transporte").click()
    const labels = await page.getByRole("option").allInnerTexts()
    await page.keyboard.press("Escape")
    return labels
  }

  // Geral: every environment, both flags, each announced with its hint.
  expect(await environments()).toEqual(["Seco / Ambiente", "Resfriado (Positivo)", "Congelado (Negativo)"])
  await expect(dangerous).toHaveAttribute("aria-checked", "false")
  await dangerous.click()
  await expect(dangerous).toHaveAttribute("aria-checked", "true")
  await dangerous.click()
  await expect(dangerous).toHaveAttribute("aria-checked", "false")
  await expect(fragile).toHaveAccessibleDescription("Exige carroceria fechada e fixação.")

  // A fragile flag doesn't survive a switch to a category that can't be fragile.
  await fragile.click()
  await expect(fragile).toHaveAttribute("aria-checked", "true")

  // Granel Líquido: tanks chill but never freeze, and bulk is never fragile.
  await chooseFormOption(page, "Categoria", "Granel Líquido")
  expect(await environments()).toEqual(["Seco / Ambiente", "Resfriado (Positivo)"])
  await expect(page.getByText("Até resfriado: nenhuma carroceria desta categoria congela.")).toBeVisible()
  await expect(fragile).toBeDisabled()
  await expect(fragile).toHaveAttribute("aria-checked", "false")
  await expect(fragile).toHaveAccessibleDescription("Só Geral ou Conteinerizada.")

  // Carga Viva: dry only, and live animals are never dangerous goods.
  await chooseFormOption(page, "Categoria", "Carga Viva")
  expect(await environments()).toEqual(["Seco / Ambiente"])
  await expect(page.getByText("Somente seco: nenhuma carroceria desta categoria refrigera.")).toBeVisible()
  await expect(dangerous).toBeDisabled()
  await expect(dangerous).toHaveAccessibleDescription("Indisponível para Carga Viva.")
})
