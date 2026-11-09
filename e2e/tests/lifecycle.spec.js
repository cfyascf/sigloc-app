// The whole freight lifecycle across both roles, through the UI:
// segment → auction → carrier bids → contractor awards → pickup, occurrence and delivery with proof
// → contractor follows the trip and downloads the proof → both histories.
// Leaves a delivered trip behind on every run (by decision: the suite always runs the full flow).
const path = require("node:path")
const fs = require("node:fs")
const { test, expect } = require("@playwright/test")
const { AUTH, env } = require("../support/env")
const { createSegment, createAuction, auctionCard, openBidForm, submitBid, awardBestBid, isApi } = require("../support/flows")
const { openNotifications, unreadCount, bell } = require("../support/ui")
const { auctionShortId } = require("../support/data")

const fixture = (name) => path.join(__dirname, "..", "fixtures", name)
const CEILING = 3000
const TOLL = 150

test.describe.configure({ mode: "serial" })

let contractor
let carrier
const run = {}

test.beforeAll(async ({ browser }) => {
  contractor = await (await browser.newContext({ storageState: AUTH.contractor })).newPage()
  carrier = await (await browser.newContext({ storageState: AUTH.carrier })).newPage()
})

test.afterAll(async () => {
  await contractor?.context().close()
  await carrier?.context().close()
})

test("contractor opens an auction", async () => {
  const segment = await createSegment(contractor, { ceiling: CEILING, toll: TOLL, pickupInHours: 30, deliveryInHours: 54 })
  Object.assign(run, { segment }, await createAuction(contractor, [segment.shortId], 20))
  await contractor.goto("/freights-offered-overview")
  await expect(auctionCard(contractor, run.shortId)).toContainText("Curitiba")
})

test("carrier is notified and sees the offer on the board", async () => {
  await carrier.goto("/carrier-dashboard")
  const panel = await openNotifications(carrier)
  await expect(panel).toContainText("abriu um leilão")
  await carrier.keyboard.press("Escape")

  await carrier.goto("/freights-offers-overview")
  await expect(carrier.getByText(`#${run.shortId}`)).toBeVisible()
})

test("bid form: pre-fill fits the ceiling and invalid values are refused", async () => {
  await openBidForm(carrier, run.auctionId)
  // Pre-filled with the highest freight that fits: ceiling minus toll.
  await expect(carrier.locator("input[type=number]").first()).toHaveValue(String(CEILING - TOLL))
  await expect(carrier.getByText(/acima do teto/)).toHaveCount(0)

  await carrier.locator("button").filter({ hasText: env.bidPlate }).first().click()
  const value = carrier.locator("input[type=number]").first()
  const confirm = carrier.getByRole("button", { name: "Confirmar Lance" })

  await value.fill("0")
  await confirm.click()
  await expect(carrier.getByText("Informe um valor de proposta maior que zero.").first()).toBeVisible()

  // One cent over the ceiling shows the cents and is refused by the server.
  await value.fill(String(CEILING - TOLL + 0.01))
  await expect(carrier.getByText("R$ 0,01 acima do teto do leilão.")).toBeVisible()
  const refused = carrier.waitForResponse((r) => isApi(r, "POST", /\/bids$/))
  await confirm.click()
  expect((await refused).status()).toBe(400)
  await expect(carrier.getByText(/Valor acima do teto do leilão/).first()).toBeVisible()
})

test("carrier bids, then lowers the bid", async () => {
  let response = await submitBid(carrier, run.auctionId, { freight: CEILING - TOLL })
  expect(response.status(), await response.text()).toBe(201)
  await expect(carrier.getByText("Lance enviado com sucesso!").first()).toBeVisible()

  response = await submitBid(carrier, run.auctionId, { freight: CEILING - TOLL - 200 })
  expect(response.status()).toBe(201)
  await expect(carrier.getByText("Lance atualizado com sucesso!").first()).toBeVisible()

  await carrier.goto(`/bid-analysis/${run.auctionId}`)
  await expect(carrier.getByText("Você lidera o leilão")).toBeVisible()
})

test("contractor sees the bid, the same ANTT floor, and awards it", async () => {
  await contractor.goto("/contractor-dashboard")
  const panel = await openNotifications(contractor)
  await expect(panel).toContainText("Lance atualizado")
  await contractor.keyboard.press("Escape")

  await contractor.goto(`/auction-bids/${run.auctionId}`)
  await expect(contractor.getByText(env.bidPlate).first()).toBeVisible()
  await expect(contractor.getByText(`R$ ${(CEILING - 200).toLocaleString("pt-BR")}`).first()).toBeVisible()

  run.tripId = await awardBestBid(contractor, run.auctionId)
  await expect(contractor).toHaveURL(/\/active-routes/)
  await expect(contractor.getByText(/Viagem TRP-[0-9A-F]{6} criada/).first()).toBeVisible()
  await expect(contractor.getByText(run.tripId)).toHaveCount(0)

  // The auction left "Em andamento".
  await contractor.goto("/freights-offered-overview")
  await expect(auctionCard(contractor, run.shortId)).toHaveCount(0)
})

test("carrier wins, and the tracking screen explains a vehicle without GPS", async () => {
  await carrier.goto("/carrier-dashboard")
  const panel = await openNotifications(carrier)
  await expect(panel).toContainText("Você venceu o leilão")
  await carrier.keyboard.press("Escape")

  await carrier.goto(`/carrier-route-tracking/${run.tripId}`)
  await expect(carrier.getByText("AGUARDANDO COLETA").first()).toBeVisible()
  const ping = carrier.getByRole("button", { name: /Transmitir Posição GPS/ })
  if (env.bidPlateHasTracker) {
    await expect(ping).toBeEnabled()
  } else {
    await expect(carrier.getByText("Rastreamento GPS indisponível")).toBeVisible()
    await expect(carrier.getByText(/Cadastre o ID do dispositivo em/)).toBeVisible()
    await expect(ping).toBeDisabled()
  }
})

test("carrier reports an occurrence and confirms the pickup", async () => {
  await carrier.goto(`/carrier-route-tracking/${run.tripId}`)

  await carrier.getByRole("button", { name: "Reportar Ocorrência" }).first().click()
  const occurrence = carrier.getByRole("dialog")
  await expect(occurrence.locator("button[type=submit]")).toBeDisabled()
  await occurrence.locator("select").selectOption({ label: "Trânsito Intenso" })
  await occurrence.locator("textarea").fill('E2E <script>alert(1)</script> & "aspas" — ç')
  await occurrence.locator("button[type=submit]").click()
  await expect(carrier.getByText(/Ocorrência reportada pelo motorista: Trânsito Intenso/).first()).toBeVisible()
  await expect(carrier.getByText('E2E <script>alert(1)</script> & "aspas" — ç', { exact: false }).first()).toBeVisible()

  await carrier.getByRole("button", { name: "Confirmar Coleta" }).click()
  const pickup = carrier.getByRole("dialog")
  await pickup.locator("textarea").fill("E2E recebido na doca 3")
  const confirmed = carrier.waitForResponse((r) => isApi(r, "POST", /\/conclusao$/))
  await pickup.locator("button[type=submit]").click()
  expect((await confirmed).status()).toBe(200)
  await expect(carrier.getByText("EM CURSO").first()).toBeVisible()
})

test("delivery proof is validated; delivering minutes after pickup is accepted but flagged", async () => {
  await carrier.goto(`/carrier-route-tracking/${run.tripId}`)
  await carrier.getByRole("button", { name: "Finalizar Entrega" }).click()
  const dialog = carrier.getByRole("dialog")
  const file = dialog.locator("input[type=file]")
  const submit = dialog.locator("button[type=submit]").last()

  await expect(submit).toBeDisabled()
  await file.setInputFiles(fixture("empty.png"))
  await expect(dialog).toContainText("O arquivo selecionado está vazio.")
  await expect(submit).toBeDisabled()
  await file.setInputFiles(fixture("not-an-image.txt"))
  await expect(dialog).toContainText("Envie um arquivo JPG, PNG ou PDF.")

  // A text file renamed to .png passes the browser check but not the server's content check.
  await file.setInputFiles(fixture("text-renamed.png"))
  let sent = carrier.waitForResponse((r) => isApi(r, "POST", /\/conclusao$/))
  await submit.click()
  expect((await sent).status()).toBe(400)
  await expect(dialog).toContainText("Envie o comprovante em JPG, PNG ou PDF.")

  await file.setInputFiles(fixture("proof.png"))
  sent = carrier.waitForResponse((r) => isApi(r, "POST", /\/conclusao$/))
  await submit.click()
  expect((await sent).status()).toBe(200)
  await expect(carrier.getByText("ENTREGUE").first()).toBeVisible()
  // Curitiba → São Paulo delivered a minute after pickup: logged as suspicious.
  await expect(carrier.getByText(/fora do esperado/).first()).toBeVisible()
})

test("contractor follows the trip: notifications, proof download and history", async () => {
  await contractor.goto("/contractor-dashboard")
  const before = await unreadCount(contractor)
  const panel = await openNotifications(contractor)
  await expect(panel).toContainText("Coleta confirmada")
  await expect(panel).toContainText("Confirmação suspeita")
  await expect(panel).toContainText(/Viagem TRP-[0-9A-F]{6} entregue/)

  // Clicking one opens the trip and the badge drops by one right away.
  await panel.getByText(/Viagem TRP-[0-9A-F]{6} entregue/).first().click()
  await expect(contractor).toHaveURL(new RegExp(`/active-route-tracking/${run.tripId}`))
  if (before > 0) await expect.poll(() => unreadCount(contractor), { timeout: 10_000 }).toBe(before - 1)

  await expect(contractor.getByText("Comprovantes de Entrega", { exact: false }).first()).toBeVisible()
  const download = contractor.waitForEvent("download")
  await contractor.getByRole("button", { name: /proof\.png/ }).click()
  const proof = await download
  expect(proof.suggestedFilename()).toBe("proof.png")
  expect(fs.statSync(await proof.path()).size).toBe(fs.statSync(fixture("proof.png")).size)

  // History lists the delivered trip at 100 %.
  await contractor.goto("/active-routes")
  await contractor.getByRole("tab", { name: "Histórico" }).click()
  const row = contractor.locator("text=" + run.tripId.replace(/-/g, "").slice(0, 6).toUpperCase()).first()
  await expect(row).toBeVisible()
  await expect(row.locator("xpath=ancestor::*[contains(., 'Progresso')][1]")).toContainText("100%")

  // "Marcar todas como lidas" clears the badge, and it stays cleared after a reload.
  const panelAgain = await openNotifications(contractor)
  await panelAgain.getByText("Marcar todas como lidas").click()
  await expect(bell(contractor)).toHaveAttribute("aria-label", "Notificações")
  await contractor.reload()
  await expect(bell(contractor)).toHaveAttribute("aria-label", "Notificações")
})

test("carrier history lists the delivered trip", async () => {
  await carrier.goto("/carrier-route-tracking")
  await carrier.getByRole("tab", { name: "Histórico" }).click()
  await expect(carrier.getByText(run.tripId.replace(/-/g, "").slice(0, 6).toUpperCase()).first()).toBeVisible()
})

test("the awarded auction cannot be bid on any more", async () => {
  await carrier.goto(`/bid-analysis/${run.auctionId}`)
  await expect(carrier.getByText(/não encontrad|não está mais disponível/i).first()).toBeVisible()
  // Sanity: the short code helper matches what the cards show.
  expect(auctionShortId(run.auctionId)).toBe(run.shortId)
})
