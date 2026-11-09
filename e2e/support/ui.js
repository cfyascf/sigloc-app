const { expect } = require("@playwright/test")
const { env } = require("./env")

/** Logs in through the login form and waits to leave the login page. */
async function loginThroughForm(page, { email, password }) {
  await page.goto("/login")
  await page.locator("input[type=email]").first().fill(email)
  await page.locator("input[type=password]").first().fill(password)
  await page.locator("button[type=submit]").first().click()
  await expect(page).not.toHaveURL(/\/login$/)
}

/**
 * Fails the test on uncaught page errors or API answers of 500 and above, the generic "something
 * broke" signals the QA passes watched for. Returns the collected list for extra assertions.
 */
function watchForBreakage(page) {
  const problems = []
  page.on("pageerror", (error) => problems.push(`page error: ${error.message}`))
  page.on("response", (response) => {
    if (response.url().startsWith(env.apiURL) && response.status() >= 500) {
      problems.push(`${response.request().method()} ${response.url()} → ${response.status()}`)
    }
  })
  return problems
}

/** The bell's accessible label, e.g. "Notificações (2 não lidas)". */
function bell(page) {
  return page.locator('button[aria-label^="Notificações"]').first()
}

async function unreadCount(page) {
  const label = (await bell(page).getAttribute("aria-label")) ?? ""
  return Number(label.match(/\((\d+)/)?.[1] ?? 0)
}

/** Opens the bell and returns the panel locator. */
async function openNotifications(page) {
  await bell(page).click()
  const panel = page.locator("[data-radix-popper-content-wrapper]").last()
  await expect(panel).toContainText("Notificações")
  return panel
}

/** Bearer token stored by the app (the API client reads it from localStorage). */
async function storedToken(page) {
  return page.evaluate(
    () => Object.entries(localStorage).find(([key, value]) => /token/i.test(key) && value && value.split(".").length === 3)?.[1] ?? null
  )
}

module.exports = { loginThroughForm, watchForBreakage, bell, unreadCount, openNotifications, storedToken }
