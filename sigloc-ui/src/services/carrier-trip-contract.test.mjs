import assert from "node:assert/strict"
import { after, before, test } from "node:test"
import { fileURLToPath } from "node:url"
import { createServer } from "vite"

let server
let carrier
const originalFetch = globalThis.fetch

before(async () => {
  server = await createServer({
    root: fileURLToPath(new URL("../../", import.meta.url)),
    server: { middlewareMode: true, hmr: false, watch: null },
    optimizeDeps: { noDiscovery: true, include: [] },
  })
  carrier = await server.ssrLoadModule("/src/services/carrier-trip-service.js")
  const client = await server.ssrLoadModule("/src/lib/api-client.js")
  client.setAuthTokenProvider(() => "carrier-token")
})

after(async () => {
  globalThis.fetch = originalFetch
  await server?.close()
})

function respond(body, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } })
}

const detail = {
  viagem: {
    viagemId: "trip-1", codigoReferencia: "VG-0001", statusGeral: "EM_CURSO", indicadorRisco: "WARNING",
    saudeOperacao: { statusSla: "ATENCAO", isCacheRenovado: false, distanciaTotalKm: 400 },
    paradas: [
      { id: "stop-2", ordem: 2, cidade: "São Paulo", estado: "SP", status: "PENDENTE", acoes: [{ id: "a2", tipo: "ENTREGA", produto: "Sorvete", prazo: "2026-10-01T20:00:00Z" }] },
      { id: "stop-1", ordem: 1, cidade: "Curitiba", estado: "PR", status: "EM_TRANSITO", acoes: [{ id: "a1", tipo: "COLETA", produto: "Sorvete", prazo: "2026-10-01T13:00:00Z" }] },
    ],
    linhaDoTempo: [{ cidade: "Curitiba, PR", acoes: ["COLETA · Sorvete"], status: "EM_TRANSITO" }],
    eventos: [],
  },
  embarcador: { razaoSocial: "Embarcador SA", nomeFantasia: "Embarcador", nomeContato: "Mesa Logística", telefone: "(41) 3333-0000", email: "logistica@embarcador.com.br" },
  especificacaoCarga: { ambiente: "Congelado", refrigerada: true, temperaturaMinima: -20, temperaturaMaxima: -15, paletizada: true, embalagens: ["Paletizado"], carroceria: "Baú", pesoTotalKg: 1200, volumeTotalM3: null, produtos: [{ nome: "Sorvete", quantidade: 12 }] },
  acaoAtual: { paradaId: "stop-1", tipo: "COLETA", exigeComprovante: false, prazo: "2026-10-01T13:00:00Z" },
  ocorrencias: [{ id: "occ-1", tipo: "TRANSITO_INTENSO", rotulo: "Trânsito Intenso", severidade: "WARNING", reportadaEm: "2026-10-01T12:00:00Z", paradaId: "stop-1", encerradaEm: null, aberta: true }],
  comprovantes: [{ id: "pod-1", paradaId: "stop-0", nomeArquivo: "canhoto.pdf", tipoConteudo: "application/pdf", tamanhoBytes: 18234, enviadoEm: "2026-10-01T11:00:00Z" }],
}

test("carrier list and detail use the carrier base path; ping sends refresh=true", async () => {
  const calls = []
  globalThis.fetch = async (url, options) => {
    calls.push({ url: new URL(url), options })
    return url.includes("/ativas")
      ? respond({ viagens: [{ viagemId: "trip-1", codigoReferencia: "VG-0001", embarcador: "Embarcador" }], totalItems: 1, totalPages: 1, page: 1, pageSize: 20 })
      : respond(detail)
  }
  const list = await carrier.listCarrierTrips({ risk: "WARNING" })
  await carrier.getCarrierTripDetails("trip-1")
  await carrier.getCarrierTripDetails("trip-1", { refresh: true })
  assert.equal(calls[0].url.pathname, "/api/transportador/viagens/ativas")
  assert.equal(calls[0].url.searchParams.get("risk"), "WARNING")
  assert.equal(list.trips[0].shipperName, "Embarcador")
  assert.equal(calls[1].url.pathname, "/api/transportador/viagens/trip-1/detalhes")
  assert.equal(calls[1].url.search, "")
  assert.equal(calls[2].url.search, "?refresh=true")
  assert.equal(calls[2].options.headers.Authorization, "Bearer carrier-token")
})

test("detail keeps the shared trip shape and adds per-stop timeline, action, cargo, contact, occurrences and proofs", () => {
  const trip = carrier.toCarrierTripDetails(detail)
  assert.equal(trip.reference, "VG-0001")
  assert.equal(trip.sla, "ATENCAO")
  assert.equal(trip.cacheRenewed, false)
  assert.deepEqual(trip.stops.map((stop) => stop.id), ["stop-1", "stop-2"])
  assert.deepEqual(trip.currentAction, { stopId: "stop-1", type: "COLETA", requiresProof: false, deadline: "2026-10-01T13:00:00Z" })
  assert.equal(trip.cargo.tempMin, -20)
  assert.equal(trip.cargo.totalVolumeM3, null)
  assert.deepEqual(trip.cargo.products, [{ name: "Sorvete", quantity: 12 }])
  assert.equal(trip.occurrences[0].open, true)
  assert.equal(trip.occurrences[0].severity, "WARNING")
  assert.equal(trip.proofs[0].fileName, "canhoto.pdf")
  assert.equal(trip.shipper.contactName, "Mesa Logística")
  assert.equal(carrier.toCarrierTripDetails({ ...detail, acaoAtual: null }).currentAction, null)
  assert.throws(() => carrier.toCarrierTripDetails({}), { code: "INVALID_RESPONSE" })
})

test("contact links are only offered for usable phone and e-mail values", () => {
  const contact = carrier.toShipperContact(detail.embarcador)
  assert.equal(contact.phoneHref, "tel:4133330000")
  assert.equal(contact.emailHref, "mailto:logistica@embarcador.com.br")
  assert.equal(carrier.toShipperContact({ razaoSocial: "X", telefone: "+55 (41) 3333-0000" }).phoneHref, "tel:+554133330000")
  const unsafe = carrier.toShipperContact({ razaoSocial: "X", telefone: "123", email: "a@b.com?bcc=x@y.com", nomeContato: "  " })
  assert.equal(unsafe.phoneHref, null)
  assert.equal(unsafe.emailHref, null)
  assert.equal(unsafe.contactName, null)
  assert.equal(carrier.toShipperContact(null), null)
})

test("stop completion posts multipart with the proof and trimmed note, never a JSON content type", async () => {
  const calls = []
  globalThis.fetch = async (url, options) => {
    calls.push({ url: new URL(url), options })
    return respond(detail)
  }
  const file = new File(["%PDF-1.7"], "canhoto.pdf", { type: "application/pdf" })
  await carrier.completeTripStop("trip-1", "stop-1", { file, note: "  Recebido por João  " })
  await carrier.completeTripStop("trip-1", "stop-1", { note: "   " })
  const [withProof, withoutProof] = calls
  assert.equal(withProof.url.pathname, "/api/transportador/viagens/trip-1/paradas/stop-1/conclusao")
  assert.equal(withProof.options.method, "POST")
  assert.equal("Content-Type" in withProof.options.headers, false)
  assert.ok(withProof.options.body instanceof FormData)
  assert.equal(withProof.options.body.get("comprovante").name, "canhoto.pdf")
  assert.equal(withProof.options.body.get("observacao"), "Recebido por João")
  assert.equal(withoutProof.options.body.has("comprovante"), false)
  assert.equal(withoutProof.options.body.has("observacao"), false)
})

test("occurrence report posts JSON and conflict errors keep their code", async () => {
  const calls = []
  globalThis.fetch = async (url, options) => {
    calls.push({ url: new URL(url), options })
    return respond(detail, 201)
  }
  await carrier.reportTripOccurrence("trip-1", { type: "AVARIA_MECANICA", note: "" })
  assert.equal(calls[0].url.pathname, "/api/transportador/viagens/trip-1/ocorrencias")
  assert.equal(calls[0].options.headers["Content-Type"], "application/json")
  assert.deepEqual(JSON.parse(calls[0].options.body), { tipo: "AVARIA_MECANICA" })
  globalThis.fetch = async () => respond({ error: "TRIP_STOP_NOT_ACTIONABLE", message: "Parada não é a próxima." }, 409)
  await assert.rejects(carrier.completeTripStop("trip-1", "stop-2", {}), { status: 409, code: "TRIP_STOP_NOT_ACTIONABLE" })
})

test("proof download returns the file blob from the carrier proof path", async () => {
  let requested
  globalThis.fetch = async (url) => {
    requested = new URL(url)
    return new Response("%PDF-1.7", { status: 200, headers: { "content-type": "application/pdf" } })
  }
  const blob = await carrier.downloadTripProof("trip-1", "pod-1")
  assert.equal(requested.pathname, "/api/transportador/viagens/trip-1/comprovantes/pod-1")
  assert.equal(blob.type, "application/pdf")
})
