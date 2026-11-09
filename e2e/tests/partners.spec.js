// Partner network: invite links and the partner cards of both roles. It does not create or end
// partnerships, so the demo accounts stay connected.
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")

test.describe("carrier", () => {
  test.use({ storageState: AUTH.carrier })

  test("connecting with an unknown code shows the error inline", async ({ page }) => {
    await page.goto("/partner-network")
    await page.getByPlaceholder("Ex: https://sigloc.app/invite/SIG-4821").fill("codigo-que-nao-existe-e2e")
    await page.getByRole("button", { name: "Conectar" }).click()
    await expect(page.getByText("O convite é inválido ou expirou.").first()).toBeVisible()
  })

  test("partner cards show the CNPJ and the last interaction, never an internal id", async ({ page }) => {
    await page.goto("/partner-network")
    const body = page.locator("main, body").first()
    await expect(body.getByText(/\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}/).first()).toBeVisible()
    await expect(body.getByText("Última interação", { exact: false }).first()).toBeVisible()
    await expect(body).not.toContainText(/[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}/i)
    await expect(body.getByText("Código de conexão", { exact: false })).toHaveCount(0)
  })
})

test.describe("contractor", () => {
  test.use({ storageState: AUTH.contractor })

  test("the active invite link opens the public invitation page", async ({ page, browser }) => {
    await page.goto("/partner-network")
    const generate = page.getByRole("button", { name: "Gerar Convite" })
    if (await generate.count()) await generate.click()
    const link = page.getByText(/\/convite\/[\w-]+/).first()
    await expect(link).toBeVisible()
    // Open it on the site under test: the API builds the link from its own configured front-end URL.
    const token = (await link.innerText()).match(/\/convite\/([\w-]+)/)[1]
    const url = `${env.baseURL}/convite/${token}`
    await expect(page.getByText(/Uso único/)).toBeVisible()

    // Opened logged out, as the invited carrier would.
    // An explicitly empty session: contexts created in a test otherwise inherit the spec's login,
    // and a logged-in contractor is (rightly) told that invites are for carriers.
    const anonymous = await browser.newContext({ storageState: { cookies: [], origins: [] } })
    const invite = await anonymous.newPage()
    await invite.goto(url)
    await expect(invite.getByText("Convite de parceria")).toBeVisible()
    await expect(invite.getByText(/convidou sua transportadora/)).toBeVisible()
    await anonymous.close()
  })
})
