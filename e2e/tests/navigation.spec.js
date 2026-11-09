// Every page of each role opens without crashes or 5xx answers, and the role guards hold.
const { test, expect } = require("@playwright/test")
const { AUTH } = require("../support/env")
const { watchForBreakage } = require("../support/ui")

const PAGES = {
  contractor: [
    ["/contractor-dashboard", "Visão Geral"],
    ["/freights-offered-overview", "Painel de Leilões"],
    ["/route-segment-management", "Gestão de Trechos"],
    ["/create-route-segment", "Cadastro de Trecho"],
    ["/active-routes", "Rotas Ativas"],
    ["/product-management", "Catálogo de Produtos"],
    ["/register-product", "Salvar Novo SKU"],
    ["/partner-network", "Rede de Parceiros"],
    ["/profile", "Meu Perfil"],
  ],
  carrier: [
    ["/carrier-dashboard", "Visão Geral"],
    ["/freights-offers-overview", "Mural de Fretes"],
    ["/carrier-route-tracking", "Rota em Trânsito"],
    ["/fleet-management", "Gestão de Frota"],
    ["/register-vehicle", "Cadastro de Veículo"],
    ["/partner-network", "Rede de Parceiros"],
    ["/profile", "Meu Perfil"],
  ],
}

for (const role of ["contractor", "carrier"]) {
  test.describe(`${role} pages`, () => {
    test.use({ storageState: AUTH[role] })

    for (const [path, heading] of PAGES[role]) {
      test(`${path} loads`, async ({ page }) => {
        const problems = watchForBreakage(page)
        await page.goto(path)
        await expect(page.getByText(heading).first()).toBeVisible()
        await expect(page.getByText(/Ocorreu um erro inesperado/)).toHaveCount(0)
        await expect(page.locator("body")).not.toContainText("NaN")
        expect(problems).toEqual([])
      })
    }
  })
}

test.describe("role guards", () => {
  test.use({ storageState: AUTH.contractor })

  test("a contractor opening a carrier page lands on its own dashboard", async ({ page }) => {
    await page.goto("/fleet-management")
    await expect(page).toHaveURL(/\/contractor-dashboard/)
  })

  test("the old control tower sends the contractor to Rotas Ativas", async ({ page }) => {
    await page.goto("/control-tower")
    await expect(page).toHaveURL(/\/active-routes/)
  })
})

test.describe("carrier guards", () => {
  test.use({ storageState: AUTH.carrier })

  test("a carrier opening a contractor page lands on its own dashboard", async ({ page }) => {
    await page.goto("/route-segment-management")
    await expect(page).toHaveURL(/\/carrier-dashboard/)
  })

  test("the old control tower sends the carrier to Rota em Trânsito", async ({ page }) => {
    await page.goto("/control-tower")
    await expect(page).toHaveURL(/\/carrier-route-tracking/)
  })
})

test.describe("not found states", () => {
  test.use({ storageState: AUTH.contractor })

  test("an unknown trip shows 'Viagem não encontrada'", async ({ page }) => {
    await page.goto("/active-route-tracking/00000000-0000-0000-0000-000000000000")
    await expect(page.getByText("Viagem não encontrada")).toBeVisible()
    await expect(page.locator("body")).not.toContainText("was not found")
  })

  test("a malformed auction link reads as not found, without an empty ranking", async ({ page }) => {
    await page.goto("/auction-bids/not-a-guid")
    await expect(page.getByText(/Registro não encontrado/)).toBeVisible()
    await expect(page.getByText(/Ranking de Propostas/i)).toHaveCount(0)
  })
})

test.describe("dashboards", () => {
  test.use({ storageState: AUTH.contractor })

  test("'Leilões ativos' matches the auctions in progress on the panel", async ({ page }) => {
    await page.goto("/contractor-dashboard")
    // KPI card: <p>Leilões Ativos</p><p>{value}</p>
    const kpi = page.locator("p", { hasText: /^Leilões Ativos$/ }).locator("xpath=following-sibling::p[1]")
    const active = Number((await kpi.innerText()).trim())

    await page.goto("/freights-offered-overview")
    await expect(page.getByText(/leil(ão|ões) • fim da lista|Nenhum leilão em andamento/).first()).toBeVisible()
    const footer = page.getByText(/leil(ão|ões) • fim da lista/).first()
    const listed = (await footer.count()) ? Number((await footer.innerText()).match(/\d+/)[0]) : 0
    expect(active).toBe(listed)
  })
})
