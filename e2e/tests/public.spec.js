// Screens reachable without a session: login, registration, password reset, invites, 404.
const { test, expect } = require("@playwright/test")
const { env } = require("../support/env")

test.describe("login", () => {
  test("empty fields are rejected before calling the API", async ({ page }) => {
    await page.goto("/login")
    await page.locator("button[type=submit]").first().click()
    await expect(page.getByText("Informe o e-mail.").first()).toBeVisible()
    await expect(page.getByText("Informe a senha.").first()).toBeVisible()
  })

  test("a wrong password shows a generic error", async ({ page }) => {
    await page.goto("/login")
    await page.locator("input[type=email]").first().fill(env.contractor.email)
    await page.locator("input[type=password]").first().fill("senha-errada-e2e")
    await page.locator("button[type=submit]").first().click()
    await expect(page.getByText("E-mail ou senha inválidos.").first()).toBeVisible()
    await expect(page).toHaveURL(/\/login/)
  })

  test("the e-mail is case-insensitive", async ({ page }) => {
    await page.goto("/login")
    await page.locator("input[type=email]").first().fill(env.contractor.email.toUpperCase())
    await page.locator("input[type=password]").first().fill(env.contractor.password)
    await page.locator("button[type=submit]").first().click()
    await expect(page).toHaveURL(/\/contractor-dashboard/)
  })

  test("a deep link survives the login redirect", async ({ page }) => {
    await page.goto("/fleet-management")
    await expect(page).toHaveURL(/\/login/)
    await page.locator("input[type=email]").first().fill(env.carrier.email)
    await page.locator("input[type=password]").first().fill(env.carrier.password)
    await page.locator("button[type=submit]").first().click()
    await expect(page).toHaveURL(/\/fleet-management/)
  })
})

test.describe("password reset", () => {
  test("forgot password validates the e-mail", async ({ page }) => {
    await page.goto("/forgot-password")
    await page.getByRole("button", { name: "Enviar link" }).click()
    await expect(page.getByText("Informe o e-mail.").first()).toBeVisible()
    await page.locator("input").first().fill("nao-e-email")
    await page.getByRole("button", { name: "Enviar link" }).click()
    await expect(page.getByText("Informe um e-mail válido.").first()).toBeVisible()
  })

  test("an unknown address gets the neutral answer", async ({ page }) => {
    await page.goto("/forgot-password")
    await page.locator("input").first().fill("ninguem-e2e-sigloc@example.com")
    await page.getByRole("button", { name: "Enviar link" }).click()
    await expect(page.getByText(/Se houver uma conta/).first()).toBeVisible()
  })

  test("reset rejects short, mismatched passwords and an invalid token", async ({ page }) => {
    await page.goto("/reset-password?token=e2e-invalido&email=ninguem-e2e-sigloc@example.com")
    const passwords = page.locator("input[type=password]")
    const submit = page.getByRole("button", { name: "Atualizar senha" })

    await passwords.nth(0).fill("123")
    await passwords.nth(1).fill("123")
    await submit.click()
    await expect(page.getByText("A senha deve ter ao menos 8 caracteres.").first()).toBeVisible()

    await passwords.nth(0).fill("senhaforte1")
    await passwords.nth(1).fill("outrasenha2")
    await submit.click()
    await expect(page.getByText("As senhas não coincidem.").first()).toBeVisible()

    await passwords.nth(1).fill("senhaforte1")
    await submit.click()
    await expect(page.getByText(/link de redefinição de senha é inválido ou expirou/).first()).toBeVisible()
  })
})

test("registration shows a Portuguese error on every required field", async ({ page }) => {
  await page.goto("/register")
  await page.getByRole("tab", { name: "Criar Conta" }).click()
  await page.getByRole("button", { name: "Usar e-mail e senha" }).click()
  await expect(page.locator("#companyName")).toHaveAttribute("maxlength", "150")
  await page.locator("button[type=submit]").last().click()
  await expect(page.getByText("CNPJ inválido. Confira os 14 dígitos informados.").first()).toBeVisible()
  await expect(page.getByText("Campo obrigatório.").first()).toBeVisible()
  await expect(page.getByText("E-mail inválido.").first()).toBeVisible()
  await expect(page.getByText("A senha deve ter ao menos 8 caracteres.").first()).toBeVisible()
})

test("registration refuses an e-mail already in use", async ({ page }) => {
  await page.goto("/register")
  await page.getByRole("tab", { name: "Criar Conta" }).click()
  await page.getByRole("button", { name: "Usar e-mail e senha" }).click()
  await page.locator("#cnpj").fill("11.222.333/0001-81")
  await page.locator("#companyName").fill("E2E Duplicado LTDA")
  await page.locator("#email").fill(env.contractor.email)
  await page.locator("#password").fill("SenhaE2E2026!")
  await page.locator("button[type=submit]").last().click()
  await expect(page.getByText(/já está em uso/).first()).toBeVisible()
  await expect(page).toHaveURL(/\/register/)
})

test("an invalid invite link says so", async ({ page }) => {
  await page.goto("/convite/token-invalido-e2e")
  await expect(page.getByText("Convite inválido")).toBeVisible()
  await expect(page.getByText("O convite é inválido ou expirou.")).toBeVisible()
})

test("an unknown page shows the Portuguese 404", async ({ page }) => {
  await page.goto("/esta-pagina-nao-existe")
  await expect(page.getByText("Página não encontrada")).toBeVisible()
  await expect(page.getByText(/não tem permissão para acessá-la/)).toBeVisible()
})
