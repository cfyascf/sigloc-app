const fs = require("node:fs")
const path = require("node:path")

// Credentials and targets come from e2e/.env (not committed); see .env.example.
const envFile = path.join(__dirname, "..", ".env")
if (fs.existsSync(envFile)) process.loadEnvFile(envFile)

function required(name) {
  const value = process.env[name]
  if (!value) {
    throw new Error(`Missing ${name}. Copy e2e/.env.example to e2e/.env and fill it in.`)
  }
  return value
}

const env = {
  baseURL: process.env.E2E_BASE_URL || "https://icy-flower-092597810.7.azurestaticapps.net",
  apiURL: process.env.E2E_API_URL || "https://sigloc-api-hbfzcfd6ghephmcc.northcentralus-01.azurewebsites.net",
  // Installed Chrome by default, so no Playwright browser download is needed. Set it empty to use
  // Playwright's own Chromium (`npx playwright install chromium`).
  browserChannel: process.env.E2E_BROWSER_CHANNEL ?? "chrome",
  contractor: { email: required("E2E_CONTRACTOR_EMAIL"), password: required("E2E_CONTRACTOR_PASSWORD") },
  carrier: { email: required("E2E_CARRIER_EMAIL"), password: required("E2E_CARRIER_PASSWORD") },
  // Catalogue product (dry, palletized) used to build segments, and the carrier vehicle that bids.
  productSku: process.env.E2E_PRODUCT_SKU || "PAR-001",
  bidPlate: process.env.E2E_BID_PLATE || "ABC1D23",
  // Whether that vehicle has a Traccar device; without one the tracking screens show a notice.
  bidPlateHasTracker: process.env.E2E_BID_PLATE_HAS_TRACKER === "true",
}

const AUTH = {
  contractor: path.join(__dirname, "..", ".auth", "contractor.json"),
  carrier: path.join(__dirname, "..", ".auth", "carrier.json"),
}

module.exports = { env, AUTH }
