import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let notifications
let bell
const originalFetch = globalThis.fetch

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  notifications = await server.ssrLoadModule("/src/services/notification-service.js")
  bell = await server.ssrLoadModule("/src/components/notification-bell.jsx")
  const client = await server.ssrLoadModule("/src/lib/api-client.js")
  client.setAuthTokenProvider(() => "test-token")
})

after(async () => {
  globalThis.fetch = originalFetch
  await server?.close()
})

const respond = (body, status = 200) => new Response(body === null ? null : JSON.stringify(body), { status, headers: { "content-type": "application/json" } })

test("list maps the API shape and keeps only in-app links", async () => {
  let requested
  globalThis.fetch = async (url) => {
    requested = new URL(url)
    return respond({ naoLidas: 2, itens: [
      { id: "n1", tipo: "NOVO_LANCE", titulo: "Novo lance", mensagem: "m", link: "/auction-bids/a1", lida: false, criadaEm: "2026-10-24T12:00:00Z" },
      { id: "n2", tipo: "X", titulo: "t", mensagem: "m", link: "https://evil.example", lida: true, criadaEm: null },
      { id: "n3", tipo: "X", titulo: "t", mensagem: "m", link: "//evil.example", lida: true },
    ] })
  }
  const result = await notifications.listNotifications({ limit: 5 })
  assert.equal(requested.pathname, "/api/notificacoes")
  assert.equal(requested.searchParams.get("limite"), "5")
  assert.equal(result.unread, 2)
  assert.deepEqual(result.items.map((n) => [n.id, n.link, n.read]), [["n1", "/auction-bids/a1", false], ["n2", null, true], ["n3", null, true]])
})

test("unread count and mark-read calls hit the expected endpoints", async () => {
  const calls = []
  globalThis.fetch = async (url, options) => {
    calls.push(`${options.method} ${new URL(url).pathname}`)
    return url.toString().endsWith("nao-lidas") ? respond({ naoLidas: 3 }) : respond(null, 204)
  }
  assert.equal(await notifications.getUnreadNotificationCount(), 3)
  await notifications.markNotificationRead("n1")
  await notifications.markAllNotificationsRead()
  assert.deepEqual(calls, ["GET /api/notificacoes/nao-lidas", "POST /api/notificacoes/n1/lida", "POST /api/notificacoes/lidas"])
})

test("notification time reads as relative Portuguese", () => {
  const now = Date.parse("2026-10-24T12:00:00Z")
  assert.equal(bell.formatNotificationTime("2026-10-24T11:59:30Z", now), "agora")
  assert.equal(bell.formatNotificationTime("2026-10-24T11:55:00Z", now), "há 5 minutos")
  assert.equal(bell.formatNotificationTime("2026-10-24T09:00:00Z", now), "há 3 horas")
  assert.equal(bell.formatNotificationTime("2026-10-23T12:00:00Z", now), "ontem")
  assert.equal(bell.formatNotificationTime(null, now), "")
})
