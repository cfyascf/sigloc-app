// Vehicle documents (ANTT RNTRC, driver CNH/EAR/drug test, MOPP) and the body/equipment rules of the
// vehicle form; the fleet's document shields and refrigeration markers.
const { test, expect } = require("@playwright/test")
const { AUTH } = require("../support/env")
const { randomPlate, randomCpf } = require("../support/data")
const { fillVehicle, registerVehicle, fleetRow, deleteVehicle } = require("../support/flows")

test.use({ storageState: AUTH.carrier })

const save = (page) => page.getByRole("button", { name: "Salvar Veículo" }).click()

/** Combobox of the body type, whatever it currently shows. */
const bodySelect = (page) =>
  page.locator("div").filter({ has: page.getByText("Tipo de Carroceria", { exact: true }) }).last().getByRole("combobox")

async function chooseBody(page, body) {
  await bodySelect(page).click()
  await page.getByRole("option", { name: body, exact: true }).click()
}

test("the form asks for the RNTRC, the driver's documents and the MOPP date before saving", async ({ page }) => {
  let posted = false
  page.on("request", (request) => {
    if (request.method() === "POST" && /\/api\/vehicles$/.test(new URL(request.url()).pathname)) posted = true
  })
  await fillVehicle(page, {
    plate: randomPlate(),
    driver: { name: "E2E Motorista" },
    mopp: true,
    rntrc: { number: "1234567", holder: "", document: "123" },
  })
  await save(page)
  for (const message of [
    "Informe o RNTRC com 8 dígitos.",
    "Informe o titular do RNTRC.",
    "Informe o CPF ou CNPJ do titular.",
    "Informe a categoria da CNH.",
    "Informe se a CNH tem EAR.",
    "Informe a validade do exame toxicológico.",
    "Informe a validade do MOPP.",
  ]) {
    await expect(page.getByText(message).first()).toBeVisible()
  }
  expect(posted).toBe(false)
  await expect(page).toHaveURL(/\/register-vehicle/)
})

/** A CPF whose last check digit is wrong. */
function badCpf() {
  const cpf = randomCpf()
  return cpf.slice(0, 10) + ((Number(cpf[10]) + 1) % 10)
}

const serverCases = [
  ["a holder CPF with a wrong check digit", { rntrc: { document: badCpf() } }, "Informe um CPF válido: o RNTRC de TAC é de pessoa física."],
  ["an ETC (company) RNTRC held by a CPF", { rntrc: { category: "ETC", document: randomCpf() } }, "Informe um CNPJ válido: o RNTRC de ETC e CTC é de pessoa jurídica."],
  [
    "a truck-trailer combination driven with a CNH C",
    { composition: "Cavalo + carreta", axles: "5", driver: { name: "E2E Motorista C", cnh: "C", ear: true, toxInDays: 300 } },
    /exige motorista com CNH categoria E/,
  ],
]
for (const [name, vehicle, message] of serverCases) {
  test(`the API refuses ${name}`, async ({ page }) => {
    const response = await registerVehicle(page, { plate: randomPlate(), ...vehicle })
    expect(response.status()).toBe(400)
    await expect(page.getByText(message).first()).toBeVisible()
    await expect(page).toHaveURL(/\/register-vehicle/)
  })
}

test("the body decides the equipment: Baú can't refrigerate, Prancha must secure the load", async ({ page }) => {
  await page.goto("/register-vehicle")
  const refrigeration = page.locator("#register-refrigeration")
  const securing = page.getByRole("checkbox", { name: /Possui fixação de carga/ })

  await chooseBody(page, "Baú")
  await expect(refrigeration).toBeDisabled()
  await expect(refrigeration.locator("option")).toHaveText(["Nenhuma"])

  await chooseBody(page, "Baú Refrigerado")
  await expect(refrigeration).toBeEnabled()
  await expect(refrigeration.locator("option")).toHaveText(["Resfriado", "Congelado"])

  await chooseBody(page, "Prancha")
  await expect(securing).toBeChecked()
  await expect(securing).toBeDisabled()
  await expect(page.getByText(/Obrigatória na carroceria Prancha/)).toBeVisible()
})

test("a vehicle with an expired drug test is saved, but the fleet says it can't bid and why", async ({ page }) => {
  const plate = randomPlate()
  const response = await registerVehicle(page, {
    plate,
    driver: { name: "E2E Tox Vencido", cnh: "E", ear: true, toxInDays: -5 },
  })
  expect(response.status(), await response.text()).toBe(201)
  try {
    await expect(page).toHaveURL(/\/fleet-management/)
    await expect(fleetRow(page, plate).locator('[title^="Documentação irregular"]')).toBeAttached()
    await page.getByRole("button", { name: `Ver detalhes do veículo ${plate}` }).click()
    await expect(page.getByText(/Exame toxicológico do motorista vencido em/).first()).toBeVisible()
  } finally {
    await deleteVehicle(page, plate)
  }
})

test("the fleet only shows refrigeration on bodies that can refrigerate", async ({ page }) => {
  await page.goto("/fleet-management")
  // Every row has a body marker such as "Baú · transporta Geral".
  await expect(page.locator('[title*=" · transporta "]').first()).toBeVisible()
  const bodiesShowingCold = await page.locator('span[title^="Refrigeração:"]').evaluateAll((spans) =>
    spans.map((span) => {
      let row = span.parentElement
      while (row && !row.querySelector('[title*=" · transporta "]')) row = row.parentElement
      return row?.querySelector('[title*=" · transporta "]')?.title.split(" · ")[0] ?? "?"
    })
  )
  for (const body of bodiesShowingCold) {
    expect(["Baú Refrigerado", "Tanque", "Porta-Contêiner"], `cold marker on a ${body}`).toContain(body)
  }
})
