# Sigloc API — local development

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
