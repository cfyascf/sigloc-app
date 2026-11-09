// Fleet: registration validation and limits, duplicate plates, edit (axles) and delete.
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { randomPlate, isoDate, randomCpf } = require("../support/data")
const { isApi } = require("../support/flows")

test.use({ storageState: AUTH.carrier })

async function fillVehicle(page, { plate, model = "E2E Volvo FH", axles = "3", kg = "12000", m3 = "40", phone = "" }) {
  await page.goto("/register-vehicle")
  await page.getByPlaceholder("Ex: ABC-1234").fill(plate)
  await page.getByPlaceholder("Ex: Volvo FH 540").fill(model)
  await page.getByPlaceholder("Ex: Curitiba, PR").fill("Curitiba, PR")
  await page.getByRole("combobox").filter({ hasText: "Selecione o implemento" }).click()
  await page.getByRole("option", { name: "Baú", exact: true }).click()
  await page.locator("#register-composition").selectOption("1") // Caminhão simples
  await page.locator("#axle-count").fill(axles)
  // Number inputs with placeholder "0": axles, then weight and volume.
  const capacity = page.locator('input[type=number][placeholder="0"]')
  await capacity.nth(1).fill(kg)
  await capacity.nth(2).fill(m3)
  if (phone) await page.locator("#register-phone").fill(phone)
  // A third-party RNTRC works on any account, even one without its own RNTRC on the profile.
  await page.getByRole("radio", { name: "De terceiro (agregado)" }).click()
  await page.locator("#register-rntrc-number").fill("12345678")
  await page.locator("#register-rntrc-category").selectOption("1") // TAC
  await page.locator("#register-rntrc-holder").fill("E2E Autônomo")
  await page.locator("#register-rntrc-document").fill(randomCpf())
  await page.locator("#register-rntrc-valid").fill(isoDate(300))
}

function fleetRow(page, plate) {
  return page.getByText(plate, { exact: true }).first().locator("xpath=ancestor::div[.//button][1]")
}

test("an empty registration shows Portuguese field errors", async ({ page }) => {
  await page.goto("/register-vehicle")
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  await expect(page.getByText("Informe a placa.")).toBeVisible()
  await expect(page.getByText("Informe a marca/modelo.")).toBeVisible()
  await expect(page.getByText("A capacidade de peso deve ser maior que zero.")).toBeVisible()
  await expect(page).toHaveURL(/\/register-vehicle/)
})

test("out-of-range capacity and a bad phone are rejected per field", async ({ page }) => {
  await fillVehicle(page, { plate: randomPlate(), axles: "99", kg: "999999999", m3: "99999", phone: "abc" })
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  await expect(page.getByText("A quantidade de eixos deve ser no máximo 12.")).toBeVisible()
  await expect(page.getByText("A capacidade de peso deve ser no máximo 100.000 kg.")).toBeVisible()
  await expect(page.getByText("A capacidade de volume deve ser no máximo 500 m³.")).toBeVisible()
  await expect(page.getByText(/Telefone inválido/)).toBeVisible()
  await expect(page).toHaveURL(/\/register-vehicle/)
})

test("a plate already in the fleet is refused, even typed lowercase with a dash", async ({ page }) => {
  const plate = env.bidPlate
  await fillVehicle(page, { plate: `${plate.slice(0, 3).toLowerCase()}-${plate.slice(3)}` })
  const sent = page.waitForResponse((r) => isApi(r, "POST", /\/api\/vehicles$/))
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  expect((await sent).status()).toBe(409)
  await expect(page.getByText(`Já existe um veículo cadastrado com a placa ${plate}.`).first()).toBeVisible()
})

test("register, edit axles and model, and delete a vehicle", async ({ page }) => {
  const plate = randomPlate()
  await fillVehicle(page, { plate, phone: "41999998888" })
  const created = page.waitForResponse((r) => isApi(r, "POST", /\/api\/vehicles$/))
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  expect((await created).status()).toBe(201)
  await expect(page).toHaveURL(/\/fleet-management/)
  await expect(fleetRow(page, plate)).toContainText("E2E Volvo FH")

  // Edit: axles beyond the limit are refused, valid values persist.
  await page.getByRole("button", { name: `Editar veículo ${plate}` }).click()
  const axles = page.locator('input[id$="-axles"]')
  await expect(axles).toHaveValue("3")
  await axles.fill("99")
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  await expect(page.getByText("A quantidade de eixos deve ser no máximo 12.").first()).toBeVisible()
  await axles.fill("4")
  await page.locator("input[maxlength='100']").first().fill("E2E Volvo editado")
  const saved = page.waitForResponse((r) => isApi(r, "PUT", /\/api\/vehicles\//))
  await page.getByRole("button", { name: "Salvar Veículo" }).click()
  expect((await saved).status()).toBe(204)
  await page.reload()
  await expect(fleetRow(page, plate)).toContainText("E2E Volvo editado")

  // Delete: cancel keeps it, confirm removes it for good.
  await page.getByRole("button", { name: `Excluir veículo ${plate}` }).click()
  await page.getByRole("button", { name: "Cancelar" }).first().click()
  await page.reload()
  await expect(page.getByText(plate, { exact: true })).toHaveCount(1)
  await page.getByRole("button", { name: `Excluir veículo ${plate}` }).click()
  const deleted = page.waitForResponse((r) => isApi(r, "DELETE", /\/api\/vehicles\//))
  await page.getByRole("button", { name: "Sim, Excluir" }).click()
  expect((await deleted).status()).toBe(204)
  await page.reload()
  await expect(page.getByText(plate, { exact: true })).toHaveCount(0)
})

test("the status filter has a single 'Em viagem' option", async ({ page }) => {
  await page.goto("/fleet-management")
  await page.locator("button[aria-expanded]").filter({ hasText: "Filtros" }).click()
  await expect(page.locator('select[aria-label="Filtrar por status"] option')).toHaveText([
    "Todos os status",
    "Livre",
    "Em viagem",
    "Manutenção",
  ])
})
