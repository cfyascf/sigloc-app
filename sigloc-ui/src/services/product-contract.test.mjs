import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let products
let apiClient

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  products = await server.ssrLoadModule("/src/services/product-service.js")
  ;({ apiClient } = await server.ssrLoadModule("/src/lib/api-client.js"))
})

after(async () => {
  await server?.close()
})

test("the product list keeps the temperature range the segment picker compares", async () => {
  const realGet = apiClient.get
  apiClient.get = async () => ({
    items: [
      { id: "p1", sku: "MOCK-CONG-01", category: "Geral", transportEnvironment: "Congelado", tempMin: -18, tempMax: -12 },
      { id: "p2", sku: "QA-SECO-01", category: "Geral", transportEnvironment: "Seco" },
    ],
  })
  try {
    const { items } = await products.listProducts({ page: 1, pageSize: 100 })
    assert.equal(items[0].tempMin, -18)
    assert.equal(items[0].tempMax, -12)
    assert.equal(items[1].tempMin, null)
    assert.equal(items[1].tempMax, null)
  } finally {
    apiClient.get = realGet
  }
})
