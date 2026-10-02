# Sigloc API — local development

The backend targets **.NET 10** and PostgreSQL 16.

## Active-trip monitoring

The contractor-facing monitoring API uses the existing JWT authentication and
shipper-access policy. Every query is scoped to the contractor's company.

- `GET /api/viagens/ativas` reads stored trip/monitoring data only. It includes
  awaiting-pickup and in-transit trips, supports search/filtering and pagination
  (20 per page by default, at most 100), and never calls a tracking or map provider.
- `GET /api/viagens/{id}/detalhes` uses the **trip GUID**, not the route GUID. It
  returns the stored snapshot until the last successful calculation is 15 minutes
  old. Opening details, including the UI's locate-driver button, is the only
  refresh trigger; there is no force-refresh bypass or background fleet scan.

Successful calculation time and the GPS fix timestamp are separate. A GPS fix
older than 15 minutes is unusable by default. A failed provider refresh preserves
all prior monitoring state and returns an explicit stale-data warning; without a
previous snapshot the API returns 503. Unknown monitoring/contact values are null,
not invented coordinates, times, phone numbers, or on-time guarantees.

### Calculation and stop semantics

OpenRouteService's driving-HGV matrix supplies directed distances and durations.
References to “OSRM” in the original story do not require a second routing engine.
Remaining distance includes every remaining stop and the connecting legs. Progress
uses the immutable consolidated-route distance and is bounded between 0 and 100%.
The displayed ETA is for the **next pending stop**, including a configurable
30-minute dock-time buffer; SLA is late only when ETA is later than that stop's
applicable deadline. Capacity utilization follows the route-volume / vehicle-volume
formula, not a reconstructed live onboard load.

At a successful on-demand observation, only the next ordered physical stop can
complete when the vehicle is inside its configurable **200 m** geofence. The origin
starts the trip and the last stop finishes it. Completion is GPS-inferred, **not
proof of loading/unloading**. A stop visited entirely between detail requests can
be missed. No automatic background detection or manual correction workflow is
included. Repeated observations must not advance additional stops.

New routes preserve the segment order selected when the auction is created.
Legacy routes have no recorded order; their documented fallback is pickup deadline
then ID, without rewriting their immutable distance. Consecutive city visits can
be grouped for display while different physical stops and return visits remain
separate operational events.

Each committed successful refresh stores a telemetry sample and factual monitoring
events atomically with the cache and any inferred lifecycle changes. A routing
matrix does not prove a traffic incident, so event messages do not fabricate live
traffic reports. The UI shows coordinates and observation time; an interactive map
and historical telemetry browsing are outside this release.

API contracts: [active list](../api-mappings/api-mapping-active-routes.md) and
[trip details](../api-mappings/api-mapping-active-route-tracking.md).

### Provider configuration

Supply tracking/routing credentials through .NET User Secrets or deployment
configuration, never source control. Configure each vehicle's optional
`traccarDeviceId` and `driverPhone` through its existing API or vehicle form.
The device ID is the Traccar numeric device identifier, not its position ID.

| Setting | Purpose / default |
| --- | --- |
| `Monitoring:MockMode` | Explicit local/test provider mode; false by default |
| `Monitoring:TraccarBaseUrl` | Traccar server URL, for example `https://demo.traccar.org` |
| `Monitoring:TraccarToken` | Bearer token; external secret only |
| `Monitoring:MaxGpsAgeMinutes` | Maximum usable GPS age: 15 |
| `Monitoring:GeofenceRadiusMeters` | Next-stop completion radius: 200 |
| `Monitoring:DockBufferMinutes` | Next-stop ETA dock allowance: 30 |
| `Monitoring:ProviderTimeoutSeconds` | Bounded upstream request timeout: 15 |
| `Monitoring:RoutingChunkSize` | Maximum matrix locations per chunk: 50 |
| `Monitoring:MockLatitude`, `Monitoring:MockLongitude` | Explicit mock GPS coordinates |
| `Monitoring:MockSpeedKmPerHour` | Mock routing speed: 60 |
| `OpenRouteService:BaseUrl` | Existing routing host, default `https://api.heigit.org` |
| `OpenRouteService:ApiKey` | Routing authorization credential; external secret only |

Example **local-only** PowerShell setup before starting the API:

```powershell
$env:Monitoring__MockMode = 'true'
$env:Monitoring__MockLatitude = '-23.5505'
$env:Monitoring__MockLongitude = '-46.6333'
dotnet run --project .\Sigloc.Api
```

Do not enable mock mode in production. For live tracking, turn it off and supply
the real provider settings externally. The frontend only calls the Sigloc API;
provider keys and raw upstream responses never belong in frontend environment
variables. Set `VITE_API_BASE_URL` to the local API URL when testing the frontend
so it does not accidentally use the deployed API.

### Monitoring tests

The solution's tests use fake HTTP handlers and controlled time for provider/cache
behavior. PostgreSQL-specific integration tests use `SIGLOC_TEST_CONNECTION`;
point it at a **disposable test database only**, never a development/shared or
production database. CI provisions a dedicated PostgreSQL service for these tests.

```powershell
# Supply a connection string for an isolated test database outside source control.
$env:SIGLOC_TEST_CONNECTION = '<disposable PostgreSQL test connection>'
dotnet test .\Sigloc.slnx --configuration Release
```

## Running against a local Postgres container

The network used by some environments cannot reach the hosted Neon database. For
local development, run Postgres in a container and point the API at it.

### 1. Start Postgres

```bash
cd sigloc-api
docker compose up -d
```

This starts a `postgres:16-alpine` container on `localhost:5432` with database
`sigloc`, user `sigloc`, password `sigloc`. Data is stored in the named volume
`sigloc-pgdata`, so it survives container restarts.

To start completely fresh (wiping all data so the seeder runs again):

```bash
docker compose down -v
docker compose up -d
```

### 2. Point the API at the local database (do NOT commit this)

The connection string is resolved from configuration, so you only override it
locally. Use .NET User Secrets (stored outside the repository):

```bash
cd sigloc-api/Sigloc.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sigloc;Username=sigloc;Password=sigloc;"
# JWT signing key (any sufficiently long random string works locally)
dotnet user-secrets set "Jwt:SecretKey" "local-dev-signing-key-change-me-0123456789"
```

> `ConnectionStrings:DefaultConnection` and `Jwt:SecretKey` are intentionally
> **blank** in `appsettings.json` so no credentials are committed. Supply them via
> User Secrets locally, and via App Settings / Key Vault on Azure.

Alternatively, set an environment variable (also outside the repo):

```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=sigloc;Username=sigloc;Password=sigloc;"
```

### 3. Run the API

```bash
cd sigloc-api/Sigloc.Api
dotnet run
```

On startup **in Development** the API automatically:

1. Applies all pending EF Core migrations (creates the schema).
2. Seeds a realistic sample dataset — but only if the database is empty.

### Seeded login

| Field    | Value                 |
| -------- | --------------------- |
| Email    | `operator@sigloc.dev` |
| Password | `Password123!`        |

The seed also creates products, several **available** route segments (for testing
`POST /api/routes/preview` and `POST /api/auctions`), and one consolidated route
already **in auction** with its two routed segments.

## Configuration precedence

The connection string (and any other setting) is resolved in this order, each
overriding the previous:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment variables

This is why the same code works locally and on Azure without changes: locally you
supply the value via User Secrets / env var; on Azure you set the App Service
setting `ConnectionStrings__DefaultConnection` (ideally backed by Key Vault).

> A GitHub repository **secret** is only available inside GitHub Actions workflows
> (e.g. for running migrations in CI). It is **not** injected into the app at
> runtime, so it is not the mechanism for local or Azure runtime configuration.

## Applying migrations to a remote database (e.g. Neon)

From a machine/network that can reach the database:

```bash
cd sigloc-api
dotnet ef database update --project Sigloc.Infrastructure --startup-project Sigloc.Api
```

Or generate an idempotent script and run it in the Neon SQL Editor:

```bash
dotnet ef migrations script --idempotent \
  --project Sigloc.Infrastructure --startup-project Sigloc.Api \
  --output migration.sql
```
