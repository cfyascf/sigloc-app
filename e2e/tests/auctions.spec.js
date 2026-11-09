// Auction management by the contractor: workspace validation, editing the end time and undoing.
const { test, expect } = require("@playwright/test")
const { AUTH } = require("../support/env")
const { localDateTime, auctionShortId } = require("../support/data")
const { createSegment, segmentCard, deleteSegment, openRouteWorkspace, startAuction, auctionCard, isApi } = require("../support/flows")

test.use({ storageState: AUTH.contractor })

test("workspace errors, end-time edit and undo give the segment back", async ({ page }) => {
  // Pickup in 30 h, so the auction must end before that.
  const segment = await createSegment(page, { pickupInHours: 30, deliveryInHours: 54 })
  let undone = false
  try {
    await openRouteWorkspace(page, [segment.shortId])
    await expect(page.getByText(/Ao fim do cronômetro, o sistema adjudica o lance de menor valor total/)).toBeVisible()

    // Ending after the first pickup: the reason shows under the end-time field.
    let response = await startAuction(page, 40)
    expect(response.status()).toBe(400)
    await expect(page.locator("#workspace-deadline-error")).toContainText("antes do primeiro prazo de coleta")

    // Ending in the past.
    response = await startAuction(page, -2)
    expect(response.status()).toBe(400)
    await expect(page.locator("#workspace-deadline-error")).toContainText("deve estar no futuro")

    // Valid.
    response = await startAuction(page, 20)
    expect(response.status(), await response.text()).toBe(201)
    const auctionId = (await response.json()).auction.id
    const shortId = auctionShortId(auctionId)

    await page.goto("/freights-offered-overview")
    const card = auctionCard(page, shortId)
    await expect(card).toContainText("Curitiba")

    // Editing the end time to the past shows the API's reason; a valid extension persists.
    await card.hover()
    await card.locator("button").nth(0).click()
    await card.locator("input[type=datetime-local]").fill(localDateTime(-24))
    await card.getByRole("button", { name: "Salvar" }).click()
    await expect(card).toContainText("O prazo de encerramento deve estar no futuro.")
    await card.locator("input[type=datetime-local]").fill(localDateTime(22))
    const saved = page.waitForResponse((r) => isApi(r, "PUT", /\/api\/auctions\//))
    await card.getByRole("button", { name: "Salvar" }).click()
    expect((await saved).status()).toBe(200)

    // Undo: dismissing keeps it, confirming moves it to the history as "Desfeito".
    await page.goto("/freights-offered-overview")
    await auctionCard(page, shortId).hover()
    await auctionCard(page, shortId).locator("button").nth(1).click()
    await auctionCard(page, shortId).getByRole("button", { name: "Não" }).click()
    await page.reload()
    await expect(auctionCard(page, shortId)).toBeVisible()
    await auctionCard(page, shortId).hover()
    await auctionCard(page, shortId).locator("button").nth(1).click()
    const undoneResponse = page.waitForResponse((r) => isApi(r, "POST", /\/undo$/))
    await auctionCard(page, shortId).getByRole("button", { name: "Sim" }).click()
    expect((await undoneResponse).status()).toBe(204)
    undone = true

    await page.reload()
    await expect(auctionCard(page, shortId)).toHaveCount(0)
    await page.getByRole("tab", { name: "Histórico" }).or(page.getByRole("button", { name: "Histórico" })).first().click()
    await expect(auctionCard(page, shortId)).toContainText(/desfeito/i)
    await expect(auctionCard(page, shortId)).not.toContainText(auctionId)

    // The segment is available again.
    await page.goto("/route-segment-management")
    await expect(segmentCard(page, segment.shortId)).toContainText(/disponível/i)
  } finally {
    if (undone || !(await isRouted(page, segment.shortId))) await deleteSegment(page, segment.shortId)
  }
})

/** True when the segment is no longer in the "Disponíveis" tab (an auction still holds it). */
async function isRouted(page, shortId) {
  await page.goto("/route-segment-management")
  return (await segmentCard(page, shortId).count()) === 0
}
