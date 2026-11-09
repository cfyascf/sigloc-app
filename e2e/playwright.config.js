// @ts-check
const { defineConfig } = require("@playwright/test")
const { env } = require("./support/env")

/**
 * The suite drives the real app through the browser, so it runs one test at a time: the flows share
 * the same demo accounts and their data. Projects run in order:
 *   setup   → logs both demo accounts in once and stores the sessions
 *   e2e     → every feature spec, reusing those sessions
 *   session → logout last, because it revokes every session of the contractor account
 */
module.exports = defineConfig({
  testDir: "./tests",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 120_000,
  expect: { timeout: 20_000 },
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: env.baseURL,
    channel: env.browserChannel || undefined,
    locale: "pt-BR",
    timezoneId: "America/Sao_Paulo",
    viewport: { width: 1440, height: 900 },
    actionTimeout: 20_000,
    navigationTimeout: 45_000,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "setup", testMatch: /auth\.setup\.js/ },
    {
      name: "e2e",
      testMatch: /.*\.spec\.js/,
      testIgnore: /session\.spec\.js/,
      dependencies: ["setup"],
    },
    {
      name: "session",
      testMatch: /session\.spec\.js/,
      dependencies: ["e2e"],
    },
  ],
})
