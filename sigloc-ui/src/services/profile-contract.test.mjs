import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let profiles

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  profiles = await server.ssrLoadModule("/src/services/profile-service.js")
})

after(async () => {
  await server?.close()
})

test("profile roles from the API map to the app's roles", () => {
  assert.equal(profiles.toRole("Shipper"), "contractor")
  assert.equal(profiles.toRole("Carrier"), "carrier")
  assert.equal(profiles.toRole("CONTRATANTE"), "contractor")
  assert.equal(profiles.toRole("Transportador"), "carrier")
  assert.equal(profiles.toRole(undefined), null)
})
