---
name: run-sigloc-app
description: Start, run, stop, and drive the Sigloc app locally (sigloc-api + sigloc-ui + Postgres) and test UI flows end to end in headless Chrome. Use when asked to run the app, start the API or UI, test a feature from the UI, reproduce a bug in the browser, take a screenshot, place a bid, create a segment/route/auction, or check what the contractor or carrier sees.
---

Sigloc runs locally as three pieces: Postgres in Docker, `Sigloc.Api` on :5100 and the Vite UI on
:5173. `stack.sh` starts and stops them. `driver.mjs` drives the UI in headless Chrome from a
script you pipe on stdin, and prints screenshots, toasts and API calls.

Paths are relative to the repo root. Verified on Windows 11 with Git Bash. There's no tmux or
lsof, so the driver is stdin-scripted (one browser per run) and `stack.sh` stops ports through
PowerShell.

## Prerequisites

Docker Desktop (running), .NET 10 SDK, Node 24, Google Chrome installed in the default location
(the driver launches the system Chrome via `channel: 'chrome'`, so there's no browser download).

The API reads `Jwt:SecretKey` and `OpenRouteService:ApiKey` from user secrets. Check they're set
(this prints the key names only):

```bash
cd sigloc-api/Sigloc.Api && dotnet user-secrets list | sed 's/ = .*//'
```

If either is missing, set it with `dotnet user-secrets set` as described in `sigloc-api/README.md`.
The ORS key is real and required: saving a segment and previewing a route geocode and route
through OpenRouteService.

## Setup (once)

```bash
cd .claude/skills/run-sigloc-app && npm install --no-audit --no-fund
```

This assumes `sigloc-ui/node_modules` already exists (it did here). If it doesn't, run `npm install`
in `sigloc-ui` first (that step wasn't re-verified).

## Run (agent path)

```bash
bash .claude/skills/run-sigloc-app/stack.sh up      # ~35 s cold; idempotent
bash .claude/skills/run-sigloc-app/stack.sh status  # api/ui up + DB connections from the app
bash .claude/skills/run-sigloc-app/stack.sh down    # kills :5100/:5173, stops the container
```

`up` survives the shell exiting. Logs: `%TEMP%/sigloc/api.log` and `%TEMP%/sigloc/ui.log`.
`up` skips a server that is already listening, so after changing API code run `down` and then `up`.
The UI hot-reloads by itself.

Drive the UI by piping commands to the driver. Each run is a fresh browser, so start with `login`:

```bash
node .claude/skills/run-sigloc-app/driver.mjs <<'EOF'
login contractor
nav /create-route-segment
click button:has-text("QA-SECO-01")
click button:has-text("SKU-FRZ-002")
fill [placeholder="Ex: Curitiba, PR"] => Curitiba, PR
fill [placeholder="Ex: São Paulo, SP"] => São Paulo, SP
fill input[type=datetime-local] >> nth=0 => 2026-11-10T08:00
fill input[type=datetime-local] >> nth=1 => 2026-11-11T18:00
fill input[placeholder="0,00"] >> nth=0 => 3000
fill input[placeholder="0,00"] >> nth=1 => 100
click role=button[name="Salvar Trecho Operacional"]
wait 6000
feedback
api
shot segment-temperature-conflict
console
EOF
```

Contractor: select a segment, open the auction workspace and start the auction:

```bash
node .claude/skills/run-sigloc-app/driver.mjs <<'EOF'
login contractor
nav /route-segment-management
click role=button[name="Criar Rota"]
check div.group:has-text("3df71898") >> input[type=checkbox]
click text=Avançar para Criar Leilão
wait-for role=button[name=/Iniciar Leilão/]
fill input[type=datetime-local] => 2026-11-02T12:00
shot auction-workspace
click role=button[name=/Iniciar Leilão/]
wait 5000
api
EOF
```

Carrier: bid on an auction (use the auction id from the `POST /api/auctions` response):

```bash
node .claude/skills/run-sigloc-app/driver.mjs <<'EOF'
login carrier
nav /bid-analysis/afb172c1-e67b-4d6c-b987-3267e6a82aa1
click role=button[name="Dar Lance"]
click button:has-text("ABC1D23")
fill input[type=number] => 2000
click role=button[name="Confirmar Lance"]
wait 3000
feedback
shot carrier-bid-rejected
EOF
```

Screenshots go to `%TEMP%/sigloc-shots/<name>.png` (override with `SIGLOC_SHOTS`). Open them and
check the page actually rendered. If a command fails, the driver saves `error.png` there and
exits 1.

| command | what it does |
|---|---|
| `login contractor\|carrier` or `login <email> <password>` | Logs in through `/login` (seeded: `operator@sigloc.dev`, `carrier@sigloc.dev`, both `Password123!`) |
| `nav <path>` | Goes to a UI route and waits for the network to go idle |
| `click <sel>` / `check <sel>` | Clicks or checks the first match. Playwright selectors: CSS, `text=`, `role=button[name="…"]` or `[name=/regex/]`, `>>` chains |
| `fill <sel> => <value>` | Fills an input. `datetime-local` takes `YYYY-MM-DDTHH:mm` in the machine's local time |
| `pick <sel> => <option>` | Radix select: opens it and clicks the option, e.g. `pick role=combobox >> text="Seco / Ambiente" => Congelado (Negativo)` |
| `wait <ms>` / `wait-for <sel>` | Fixed wait, or wait up to 20 s for an element |
| `feedback` | Toasts, alert banners and red field errors currently on screen |
| `api` | API calls made since the last `api`: method, status, path, and the body for writes and errors (never auth) |
| `shot <name>` / `text [chars]` / `console` / `eval <js>` | Screenshot, body text, console errors, run JS in the page |

## Run (human path)

Run `stack.sh up`, then open http://localhost:5173 in a browser and log in with a seeded account.

## Test

```bash
cd sigloc-api && dotnet test Sigloc.Tests
```

This runs fully offline. It finished with 985 passed and 5 skipped (the Postgres-only tests).

## Gotchas

- **Never `dotnet run` the API bare.** The user-secrets connection string points at the hosted
  Neon database, and Development startup migrates and seeds whatever it connects to. `stack.sh`
  always overrides it with the local container through `ConnectionStrings__DefaultConnection`.
  `status` showing connections from the app proves it's on the local DB.
- **Local data persists** in the `sigloc-pgdata` Docker volume. The seeder only runs on an empty
  database, so seeded segments have past pickup dates ("COLETA VENCIDA") and can't be auctioned.
  Create fresh segments with future dates. The auction expiry must be before the first pickup.
- **The carrier has only one truck.** `carrier@sigloc.dev` owns `ABC1D23` and is partnered with
  `operator@sigloc.dev`. `EFG4H56` belongs to a different carrier that has no login, so it never
  shows up in the bid form.
- **The bid button changes name.** It's "Dar Lance" until the carrier bids on that auction, then
  "Atualizar Lance".
- **The segment form's product picker uses `<button>`s,** so use `button:has-text("<SKU>")`, not a
  div. Products that clash on cargo class are greyed out ahead of time. Products that clash on
  temperature are only rejected by the API when you save.
- **Geocoding is live.** Use real Brazilian "City, UF" addresses. A segment save takes about 5–7 s
  (`wait 6000`).
- **`datetime-local` uses the browser's local time.** On this machine (UTC-3), `22:00` was saved
  as `01:00+00:00`, and messages print Brasília time.
- **`api.log` is noisy.** EF logs every SQL statement at Information level. Grep it rather than
  tailing it.

## Troubleshooting

- **`locator.click: Timeout 30000ms exceeded` on a product in the segment form**: the selector
  targeted a div. Use `button:has-text("<SKU>")`.
- **The `click role=button[name="Dar Lance"]` command times out**: the carrier already bid on
  that auction, so the button reads "Atualizar Lance".
- **The harness reports a background `dotnet run` / `vite` task as "failed with exit code 127"**:
  `stack.sh down` killed servers that were started outside `stack.sh`. That's expected.
