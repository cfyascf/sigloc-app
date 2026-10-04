# Azure Deployment Plan

> **Status:** Validated

Generated: 2026-10-03

---

## 1. Project Overview

**Goal:** Add Infrastructure-as-Code (azd + Bicep) to the existing Sigloc app so the entire
Azure footprint can be recreated in a **different Azure account/tenant** with a single
`azd up`. This makes the requested account switch — and any future ones — repeatable and
scripted instead of manual portal work.

**Path:** Modernize Existing (add Azure IaC to a manually-provisioned app)

**Why this approach:** Azure cannot move resources across tenants. The only reliable way to
switch accounts is to recreate resources in the new account and repoint deployment. Codifying
the infrastructure as IaC turns that into `azd env new <account>` + `azd up`.

---

## 2. Requirements

| Attribute | Value |
|-----------|-------|
| Classification | Development / small production |
| Scale | Small |
| Budget | Cost-Optimized |
| **Subscription** | ⚠️ Confirm during execution — target subscription in the NEW account |
| **Location** | ⚠️ Confirm during execution — default `centralus` (matches current) |

---

## 3. Components Detected

| Component | Type | Technology | Path |
|-----------|------|------------|------|
| sigloc-api | API | .NET 10 Web API (EF Core + Npgsql, Serilog) | ./sigloc-api |
| sigloc-ui | Frontend | React + Vite SPA (build output `dist`) | ./sigloc-ui |
| database | Data | **External/managed PostgreSQL** (not provisioned by IaC) | connection string |

Notes:
- API applies EF migrations automatically only in Development; in the new account the schema
  arrives via the user's `pg_dump`/`pg_restore` data migration.
- API reads CORS origins, connection string, JWT, SMTP, Google, ORS from configuration /
  App Settings — no recompile needed per environment.

---

## 4. Recipe Selection

**Selected:** AZD (Bicep)

**Rationale:**
- User wants one-command recreation across accounts — `azd` provides per-environment
  subscription/tenant selection (`azd env new`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`)
  and `azd up` (provision + deploy).
- Azure-only app → Bicep is the simplest IaC provider.
- Multi-service (API on App Service + UI on Static Web App) is well supported by azd.

---

## 5. Architecture

**Stack:** App Service (API) + Static Web App (UI) + external PostgreSQL

### Service Mapping

| Component | Azure Service | SKU |
|-----------|---------------|-----|
| sigloc-api | App Service (Linux, .NET 10) | **B1 (Basic)** |
| sigloc-ui | Static Web App | **Free** |
| database | External managed PostgreSQL | n/a (connection string only) |

### Supporting Services

| Service | Purpose |
|---------|---------|
| Log Analytics | Centralized logging |
| Application Insights | Monitoring & APM for the API |
| Key Vault | Secrets: DB connection string, JWT key, SMTP password, ORS/Traccar keys |
| Managed Identity (System-assigned) | App Service → Key Vault (Key Vault Secrets User) |

### Configuration flow
- Secrets stored in Key Vault; App Service references them via
  `@Microsoft.KeyVault(VaultName=...;SecretName=...)` app settings.
- Static Web App URL and API URL are wired between services via Bicep outputs /
  app settings so CORS and frontend base URLs are environment-correct.
- Secret **values** are supplied as azd environment parameters (never committed), so each
  account/environment gets its own.

### Account-switch workflow enabled by this IaC
```
azd auth login              # log into the NEW account/tenant
azd env new <new-account>   # new isolated environment
azd env set AZURE_SUBSCRIPTION_ID <id>
azd env set AZURE_LOCATION <region>
# set secret params (DB conn string, JWT key, etc.)
azd up                      # provision + deploy everything
```

---

## 6. Provisioning Limit Checklist

### Phase 1: Prepare Resource Inventory

| Resource Type | Number to Deploy | Total After Deployment | Limit/Quota | Notes |
|---------------|------------------|------------------------|-------------|-------|
| Microsoft.Web/serverfarms (B1) | 1 | _Phase 2_ | _Phase 2_ | Linux plan |
| Microsoft.Web/sites | 1 | _Phase 2_ | _Phase 2_ | API |
| Microsoft.Web/staticSites (Free) | 1 | _Phase 2_ | _Phase 2_ | UI |
| Microsoft.KeyVault/vaults | 1 | _Phase 2_ | _Phase 2_ | RBAC vault |
| Microsoft.OperationalInsights/workspaces | 1 | _Phase 2_ | _Phase 2_ | logging |
| Microsoft.Insights/components | 1 | _Phase 2_ | _Phase 2_ | App Insights |

### Phase 2: Fetch Quotas and Validate Capacity

Runs during execution, **after** the NEW account's subscription + region are confirmed
(via the azure-quotas skill). These are all low-footprint resources well within default
limits, but will be validated against the target subscription before `azd up`.

**Status:** Pending (requires confirmed target subscription — collected at execution time)

**Notes:** No database resource is provisioned (external managed Postgres). Data is migrated
by the user via `pg_dump`/`pg_restore`.

---

## 7. Generated Artifacts

| File | Purpose |
|------|---------|
| `azure.yaml` | azd service definitions: `api` (App Service, dotnet) + `web` (Static Web App) |
| `infra/main.bicep` | Subscription-scoped orchestrator (creates RG + all modules) |
| `infra/main.parameters.json` | Maps azd env vars → Bicep params (secrets via env, never committed) |
| `infra/modules/monitoring.bicep` | Log Analytics + Application Insights |
| `infra/modules/keyvault.bicep` | RBAC Key Vault + secrets (DB, JWT, SMTP, ORS, Traccar) |
| `infra/modules/keyvault-access.bicep` | Key Vault Secrets User role for API managed identity |
| `infra/modules/appservice.bicep` | Linux B1 App Service (.NET 10) + system-assigned identity + KV-referenced app settings |
| `infra/modules/staticwebapp.bicep` | Free Static Web App |
| `.gitignore` | Prevents committing azd per-environment secrets under `.azure/<env>/` |

**Validation performed:** `bicep build infra/main.bicep` compiles cleanly (no warnings or errors).

### Secret parameters to set per environment (via `azd env set`)
- `DATABASE_CONNECTION_STRING` (required)
- `JWT_SECRET_KEY` (required)
- `SMTP_PASSWORD` (optional)
- `OPENROUTESERVICE_API_KEY` (optional)
- `TRACCAR_TOKEN` (optional)
- `GOOGLE_CLIENT_ID` (optional, non-secret)

---

## 8. Validation (azure-validate)

### Validation Steps (AZD / Bicep recipe)
- [x] 1. AZD config present (`azure.yaml`) + infra files present (`./infra/*.bicep`)
- [x] 2. Schema / structure validation of `azure.yaml`
- [x] 3. Service source paths exist (`./sigloc-api/Sigloc.Api`, `./sigloc-ui`)
- [x] 4. Bicep compiles (`bicep build infra/main.bicep`)
- [x] 5. Build verification (`dotnet build` API, `npm run build` UI)
- [x] 6. Static RBAC review (Key Vault Secrets User for API identity)
- [ ] 7. Environment / Auth / Subscription — **deferred to deploy time** (requires login to the NEW account; by design not available yet)
- [ ] 8. `azd provision --preview` / `azd package` — **deferred to deploy time** (requires the new-account subscription)

> The deferred checks are account-dependent and will run during the actual
> account switch (`azd up` under azure-deploy) once the new subscription is set.

### Validation Proof

| Check | Command | Result |
|-------|---------|--------|
| Bicep compiles | `bicep build infra/main.bicep` | ✅ No warnings or errors |
| API builds | `dotnet build Sigloc.slnx -c Release` | ✅ Build succeeded, 0 errors |
| UI builds | `npm run build` (sigloc-ui) | ✅ Built `dist/` in ~22s |
| azure.yaml structure | manual review vs azd schema | ✅ Two services (api/appservice, web/staticwebapp) |
| Source paths | file existence | ✅ `sigloc-api/Sigloc.Api`, `sigloc-ui` present |
| RBAC static review | review `keyvault-access.bicep` | ✅ Key Vault Secrets User → API system-assigned identity |
| Build artifacts not committed | `git status` | ✅ `dist/`, `bin/` gitignored |

**Note:** `azd` CLI is not installed in the prep environment and the new-account
subscription is not yet configured, so `azd provision --preview` / `azd package`
are deferred to deploy time (the actual account switch).
