// Logs both demo accounts in once; the specs reuse the stored sessions.
const { test: setup, expect } = require("@playwright/test")
const { env, AUTH } = require("../support/env")
const { loginThroughForm } = require("../support/ui")

for (const [role, landing] of [["contractor", /\/contractor-dashboard/], ["carrier", /\/carrier-dashboard/]]) {
  setup(`log in as ${role}`, async ({ page }) => {
    await loginThroughForm(page, env[role])
    await expect(page).toHaveURL(landing)
    await page.context().storageState({ path: AUTH[role] })
  })
}
