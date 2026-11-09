// Logout revokes the token on the server. Runs last (own project): "Sair" ends every session of
// the contractor account, including the one the other specs reuse.
const { test, expect } = require("@playwright/test")
const { env } = require("../support/env")
const { loginThroughForm, storedToken } = require("../support/ui")

test("'Sair' ends the session in the app and on the server", async ({ page, request }) => {
  await loginThroughForm(page, env.contractor)
  const token = await storedToken(page)
  expect(token).toBeTruthy()

  const profile = () => request.get(`${env.apiURL}/api/profile`, { headers: { Authorization: `Bearer ${token}` } })
  expect((await profile()).status()).toBe(200)

  await page.locator("button").filter({ hasText: "CONTRATANTE" }).first().click()
  await page.getByText("Sair").first().click()
  await expect(page).toHaveURL(/\/login/)
  expect(await storedToken(page)).toBeNull()

  // The old token no longer works, and protected pages send back to the login.
  await expect.poll(async () => (await profile()).status(), { timeout: 10_000 }).toBe(401)
  await page.goto("/route-segment-management")
  await expect(page).toHaveURL(/\/login/)

  // A fresh login works again.
  await loginThroughForm(page, env.contractor)
  await expect(page).toHaveURL(/\/contractor-dashboard|\/route-segment-management/)
})
