// "Meu Perfil": role-specific sections, validation, and the header showing the company name.
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { isoDate } = require("../support/data")

test.describe("contractor profile", () => {
  test.use({ storageState: AUTH.contractor })

  test("shows the logistics contact section and limits the company name", async ({ page }) => {
    await page.goto("/profile")
    await expect(page.getByText("Contato Logístico")).toBeVisible()
    await expect(page.locator("#companyName")).toHaveAttribute("maxlength", "150")
    await expect(page.locator("#tradeName")).toHaveAttribute("maxlength", "150")
    await expect(page.locator("#cnpj")).toBeDisabled()
    await expect(page.locator("#cnpj")).toHaveValue(/^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$/)
  })

  test("an empty company name is rejected and nothing changes", async ({ page }) => {
    await page.goto("/profile")
    const original = await page.locator("#companyName").inputValue()
    await page.locator("#companyName").fill("")
    await page.getByRole("button", { name: "Salvar Alterações" }).click()
    await expect(page.getByText("Campo obrigatório.").first()).toBeVisible()
    await page.reload()
    await expect(page.locator("#companyName")).toHaveValue(original)
  })

  test("saving keeps the data and the header shows the company, not the e-mail", async ({ page }) => {
    await page.goto("/profile")
    const tradeName = await page.locator("#tradeName").inputValue()
    const companyName = await page.locator("#companyName").inputValue()
    await page.getByRole("button", { name: "Salvar Alterações" }).click()
    await expect(page.getByText("Dados atualizados com sucesso.")).toBeVisible()
    const shownName = tradeName || companyName
    await expect(page.locator("aside, header").getByText(shownName, { exact: true }).first()).toBeVisible()
    const emailPrefix = env.contractor.email.split("@")[0]
    if (emailPrefix !== shownName) {
      await expect(page.locator("aside, header").getByText(emailPrefix, { exact: true })).toHaveCount(0)
    }
  })
})

test.describe("carrier profile", () => {
  test.use({ storageState: AUTH.carrier })

  test("shows the operational indicators and no logistics contact", async ({ page }) => {
    await page.goto("/profile")
    await expect(page.getByText("Indicadores Operacionais")).toBeVisible()
    await expect(page.getByText("Contato Logístico")).toHaveCount(0)
  })
})

test.describe("carrier RNTRC validity", () => {
  test.use({ storageState: AUTH.carrier })

  async function saveValidity(page, date) {
    await page.goto("/profile")
    await page.locator("#rntrcValidUntil").fill(date)
    const saved = page.waitForResponse((r) => ["PUT", "PATCH"].includes(r.request().method()) && /\/api\/profile/.test(r.url()))
    await page.getByRole("button", { name: "Salvar RNTRC" }).click()
    expect((await saved).ok()).toBe(true)
  }

  test("warns before and after the company's RNTRC expires, and the warning goes away when renewed", async ({ page }) => {
    await page.goto("/profile")
    test.skip(!(await page.locator("#rntrcNumber").inputValue()), "the carrier account has no RNTRC of its own")
    const original = await page.locator("#rntrcValidUntil").inputValue()
    const rntrcAlerts = page.locator("[role=alert], [role=status]").filter({ hasText: "RNTRC" })
    try {
      await saveValidity(page, isoDate(10))
      await expect(rntrcAlerts).toContainText(/RNTRC vence em .+ \(em \d+ dias\)/)

      // Two days back, so it is expired whatever the hour in Brasília.
      await saveValidity(page, isoDate(-2))
      await expect(page.locator("[role=alert]").filter({ hasText: "RNTRC" })).toContainText(/RNTRC vencido em .+ não podem dar lances/)
    } finally {
      await saveValidity(page, original || isoDate(365))
    }
    // Back to the real date: no warning, unless that date is itself within 30 days.
    if (!original || original > isoDate(31)) await expect(rntrcAlerts).toHaveCount(0)
  })
})
