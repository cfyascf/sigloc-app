# Sigloc end-to-end tests

Playwright tests that drive the deployed app through the browser, as a contractor and as a carrier.
They replay the exploratory QA passes of October and November 2026: every flow is done through the screens, and
the assertions check what a user sees.

## Run

```bash
cd e2e
npm install
cp .env.example .env   # then fill in the two demo accounts
npm test               # headless, about 10 minutes
npm run test:headed    # watch it in a browser window
npm run report         # open the HTML report of the last run
```

Against the local stack (`.claude/skills/run-sigloc-app/stack.sh up`), with the seeded accounts:

```bash
cat > .env.local-stack <<'X'
E2E_BASE_URL=http://localhost:5173
E2E_API_URL=http://localhost:5100
E2E_CONTRACTOR_EMAIL=operator@sigloc.dev
E2E_CONTRACTOR_PASSWORD=Password123!
E2E_CARRIER_EMAIL=carrier@sigloc.dev
E2E_CARRIER_PASSWORD=Password123!
E2E_PRODUCT_SKU=QA-SECO-01
E2E_BID_PLATE=DOC1A23
X
set -a; . ./.env.local-stack; set +a; npm test
```

Variables already set in the shell win over `.env`, so this doesn't touch the production settings.

Run a single file or test:

```bash
npx playwright test tests/lifecycle.spec.js
npx playwright test -g "cargo classes"
```

On a failure the report keeps a trace (`npx playwright show-trace <file>`), with a screenshot of each
step and the network calls.

## What it covers

| File | Covers |
|---|---|
| `public.spec.js` | Login validation and errors, deep link after login, password reset, registration errors and duplicate e-mail, invalid invite, 404 page |
| `navigation.spec.js` | Every page of both roles opens with no crash or 5xx; role guards; `/control-tower` redirect; "Viagem não encontrada"; the "Leilões ativos" KPI matches the auction panel |
| `profile.spec.js` | Logistics contact (contractor) and indicators (carrier); required and length-limited company name; header shows the company name; the carrier's RNTRC warning when it is about to expire and once expired (the date is put back afterwards) |
| `products.spec.js` | Field errors; case-insensitive duplicate SKU; create, edit, cancel, delete; environments and hazard flags per category; the hazard toggles are named checkboxes with their hint |
| `fleet.spec.js` | Field errors; capacity, axle and phone limits; duplicate plate; create, edit axles, delete; single "Em viagem" filter |
| `vehicle-documents.spec.js` | RNTRC, driver (CNH, EAR, drug test) and MOPP required fields; CPF check digits, ETC needing a CNPJ and CNH E for combinations (API); body locks on refrigeration and cargo securing; an expired drug test flags the vehicle in the fleet; cold markers only on bodies that refrigerate |
| `segments.spec.js` | Every segment validation (dates, origin = destination, money, toll vs ceiling, unknown address); geocoded city on the card; edit; delete; cargo classes in "Novo Trecho" and in "Criar Rota" |
| `segment-rules.spec.js` | Product picker locks (temperature, category) and pt-BR weights; "Criar Rota" locks (one temperature, bulk offered alone); coordinates (pasted pin, swapped values, reopened in edit); the city an address resolved to, confirmed before saving when it isn't the one typed |
| `bid-vehicles.spec.js` | The bid form greys out a tank truck (wrong body) and a truck with an expired drug test, with the reason, below the usable vehicles |
| `auctions.spec.js` | End-time errors under the field; end-time edit; undo moves the auction to the history and frees the segment |
| `lifecycle.spec.js` | Segment → auction → notification → bid validation → bid and update → award → "Você venceu" → no-GPS notice → occurrence → pickup → proof validation → delivery flagged as suspicious → contractor notifications, unread badge, proof download, histories |
| `partners.spec.js` | Unknown invite code; partner cards show the CNPJ, never an internal id; the active invite opens the public invitation page |
| `session.spec.js` | "Sair" revokes the token on the server; protected pages go back to the login |

## Things to know

- **It writes to the target environment on every run.** By decision, the full lifecycle always runs.
  Each run leaves a delivered trip, a closed auction, its segment and the related notifications on the
  demo accounts. What can be removed is removed (products, vehicles, the segments of the validation,
  cargo-class, cargo-rule, location and undo tests, and the auction of the bid-form test is undone).
- **It briefly changes the carrier's RNTRC date.** The profile test saves a date 10 days ahead, then
  2 days ago, and puts the original back. While it runs, that account's own-RNTRC vehicles can't bid.
- **It logs the demo contractor out everywhere.** `session.spec.js` runs last and "Sair" revokes every
  session of that account, including one you may have open in your browser.
- **It marks the contractor's notifications as read** (the lifecycle checks "Marcar todas como lidas").
- **One test at a time.** The specs share the demo accounts, so the config runs a single worker.
- **Requirements on the accounts.** The contractor and the carrier must be partners. The catalogue
  must have `E2E_PRODUCT_SKU` (a dry, palletized product that is neither dangerous nor fragile), and the
  carrier must have `E2E_BID_PLATE` free (a "Baú / Sider" vehicle).
- **Geocoding.** Segment creation and the address check need the API to reach OpenRouteService, so
  these tests can't run against a local API on a network that blocks it. A failed save with
  `GEOCODING_ERROR` (502) is the provider or the network, not the app: run that test again.
- **New checks need the matching app version.** `vehicle-documents`, `bid-vehicles`, `segment-rules`
  and the new profile and product tests check features of the November 2026 QA fixes; against an
  environment that doesn't have them deployed yet, those tests fail.
