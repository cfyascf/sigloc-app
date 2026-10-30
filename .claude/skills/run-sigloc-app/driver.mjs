#!/usr/bin/env node
// Scripted headless-Chrome driver for the local Sigloc UI (stack.sh up must be running).
// Reads one command per line from stdin, runs them in a single browser session, and exits
// non-zero (with an error screenshot) on the first failing command.
//
//   node driver.mjs <<'EOF'
//   login contractor
//   nav /route-segment-management
//   shot segments
//   EOF
//
// Selectors are Playwright selectors: CSS, text="...", role=button[name="..."], and `>>` chains.
import { chromium } from 'playwright-core';
import { createInterface } from 'node:readline';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const UI = process.env.SIGLOC_UI ?? 'http://localhost:5173';
const API = process.env.SIGLOC_API ?? 'http://localhost:5100';
const SHOTS = process.env.SIGLOC_SHOTS ?? path.join(os.tmpdir(), 'sigloc-shots');
const ACCOUNTS = {
  contractor: ['operator@sigloc.dev', 'Password123!'],
  carrier: ['carrier@sigloc.dev', 'Password123!'],
};
// Toasts (sonner), alert banners and inline field errors.
const FEEDBACK = 'li[data-sonner-toast], [role=status], [role=alert], .text-red-500, .text-red-600, .text-rose-600, .text-destructive';

fs.mkdirSync(SHOTS, { recursive: true });
const browser = await chromium.launch({ channel: 'chrome', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'pt-BR' });
const page = await context.newPage();

const apiCalls = [];
const consoleErrors = [];
page.on('response', async (r) => {
  if (!r.url().startsWith(API) || r.request().method() === 'OPTIONS') return;
  // Bodies only for writes and errors; never for auth responses (they carry the JWT).
  const wantBody = (r.status() >= 400 || r.request().method() !== 'GET') && !r.url().includes('/api/auth/');
  const body = wantBody ? (await r.text().catch(() => '')).slice(0, 600) : '';
  apiCalls.push(`${r.request().method()} ${r.status()} ${r.url().slice(API.length)} ${body}`.trim());
});
page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text().slice(0, 300)); });
page.on('pageerror', (e) => consoleErrors.push(`[pageerror] ${e.message}`));

const shot = async (name) => {
  const file = path.join(SHOTS, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log(`  screenshot: ${file}`);
};
// "selector => value"
const split = (args) => {
  const at = args.indexOf(' => ');
  if (at < 0) throw new Error('expected "<selector> => <value>"');
  return [args.slice(0, at), args.slice(at + 4)];
};

const commands = {
  async login(args) {
    const [email, password] = ACCOUNTS[args] ?? args.split(' ');
    await page.goto(`${UI}/login`, { waitUntil: 'networkidle' });
    await page.locator('input[type=email]').fill(email);
    await page.locator('input[type=password]').fill(password);
    await page.locator('button[type=submit]').click();
    await page.waitForURL((url) => !url.pathname.startsWith('/login'), { timeout: 20000 });
    await page.waitForLoadState('networkidle');
    console.log(`  logged in as ${email} -> ${page.url().slice(UI.length)}`);
  },
  async nav(args) {
    await page.goto(UI + args, { waitUntil: 'networkidle' });
  },
  async click(args) { await page.locator(args).first().click(); },
  async check(args) { await page.locator(args).first().check(); },
  async fill(args) {
    const [selector, value] = split(args);
    await page.locator(selector).first().fill(value);
  },
  // Radix <Select>: open the combobox, then click the option by its visible name.
  async pick(args) {
    const [selector, option] = split(args);
    await page.locator(selector).first().click();
    await page.getByRole('option', { name: option, exact: true }).click();
  },
  async wait(args) { await page.waitForTimeout(Number(args)); },
  async 'wait-for'(args) { await page.locator(args).first().waitFor({ timeout: 20000 }); },
  async shot(args) { await shot(args || 'screenshot'); },
  async text(args) {
    const body = (await page.locator('body').innerText()).replace(/\n+/g, ' | ');
    console.log(`  ${body.slice(0, Number(args) || 2000)}`);
  },
  async feedback() {
    // SVG icons match the color classes too and have no innerText.
    const items = await page.locator(FEEDBACK).allInnerTexts();
    console.log(`  ${JSON.stringify(items.map((t) => (t ?? '').trim()).filter(Boolean))}`);
  },
  async api() {
    for (const call of apiCalls.splice(0)) console.log(`  ${call}`);
  },
  async console() {
    for (const error of consoleErrors.splice(0)) console.log(`  ${error}`);
  },
  async eval(args) { console.log(`  ${JSON.stringify(await page.evaluate(args))}`); },
};

let failed = false;
for await (const raw of createInterface({ input: process.stdin })) {
  const line = raw.trim();
  if (!line || line.startsWith('#')) continue;
  const space = line.indexOf(' ');
  const [name, args] = space < 0 ? [line, ''] : [line.slice(0, space), line.slice(space + 1)];
  console.log(`> ${line}`);
  try {
    if (!commands[name]) throw new Error(`unknown command "${name}" (${Object.keys(commands).join(', ')})`);
    await commands[name](args);
  } catch (error) {
    console.log(`!! ${error.message.split('\n')[0]}`);
    await shot('error').catch(() => {});
    failed = true;
    break;
  }
}

await browser.close();
process.exit(failed ? 1 : 0);
