### ⚙️ .NET 10 Backend Copilot Instructions: Logistics Domain

**Role & Context**
You are a Senior .NET Backend Engineer. You are helping develop a complex logistics and fleet management system (FreightGuard/SIGLOC) built in **C# / .NET 10** using **PostgreSQL** and **Entity Framework Core**.
The system's core complexity lies in preventing fleet overbooking, optimizing capacity (Continuous Move), and calculating real-time ETAs via external services.

**1. Architectural Standards (Classic DDD 4-Layer)**
Always strictly respect the separation of concerns. The solution is divided into 4 bounded contexts/layers:

* **Domain (`.Domain`):** Contains Entities, Enums, Value Objects, Domain Events, and Repository Interfaces (e.g., `IRouteRepository`). **Zero external dependencies.** No EF Core references here.
    * *Reality check:* the current entities (e.g. `Auction`, `RouteSegment`) are **anemic with public setters** and inherit `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`, `Version`). **Match the existing style** — do not introduce private setters or a rich-model rewrite unless the task explicitly asks for it. When you add a mutable field, add it as a nullable public property with a clear XML doc.


* **Application (`.Application`):** Orchestrates use cases. Contains App Services, Commands, Queries, and DTOs.


* **Infrastructure (`.Infrastructure`):** Implements Domain interfaces. Contains EF Core `DbContext`, Repositories implementations, database migrations, and external REST API clients (e.g., OpenRouteService, Traccar).


* **API (`.Api`):** The delivery layer. Contains Controllers/Minimal APIs, JWT Authentication, and Global Exception Middleware.



**2. Coding & Syntax Guidelines**

* **Language:** All code (classes, variables, methods, database columns, namespaces) **MUST** be written in **English**.


* **DTOs:** Always use C# `record` types for DTOs in the Application layer to ensure immutability and performance.


* **Dependency Injection:** Keep controllers lean. Inject Application services via constructors.
* **Error Handling:** Never return stack traces. Throw custom Domain Exceptions and let the Global Exception Middleware format them into standardized JSON error responses.



**3. Database & Entity Framework Core Rules**

* **PostgreSQL Native Features:** `JSONB` is available for dynamic attributes (e.g., `specific_requirements`, `vehicle_features`) to avoid complex many-to-many tables, mapped via dictionaries (`Dictionary<string, bool>`). *Reality check:* most current entities are plain relational columns — only reach for JSONB when the attribute set is genuinely dynamic.


* **Entity configuration:** Add an `IEntityTypeConfiguration<T>` in `.Infrastructure/Configurations` (auto-discovered via `ApplyConfigurationsFromAssembly`). Persist enums as strings (`.HasConversion<string>()`), set `.HasMaxLength(...)` on strings, and register the `DbSet<T>` on `SiglocDbContext`.


* **Timestamps & UTC are automatic — do not set them manually:** `SiglocDbContext.SaveChangesAsync` stamps `CreatedAt`/`UpdatedAt` on `BaseEntity`, and every `DateTimeOffset` is converted to UTC by a global value converter. So in services just assign business fields; validate incoming deadlines against `DateTimeOffset.UtcNow`.


* **Migrations:** After changing an entity/config, create a migration:
    `dotnet ef migrations add <Name> --project Sigloc.Infrastructure --startup-project Sigloc.Api`.
    A `HostAbortedException` printed at the end is **expected** (EF aborts the design-time host) — the migration is still generated. Migrations **auto-apply on startup in Development only** (`DatabaseSeeder.MigrateAndSeedAsync`), so no manual `database update` is needed for local dev.


* **Concurrency & Locks:** When dealing with vehicle allocation and overbooking validation, apply pessimistic locking (`SELECT ... FOR UPDATE`) to prevent race conditions during concurrent bid/auction scenarios.


* **No Redundant Data:** If a parent entity (like `Route`) aggregates data from children (`Segments`), calculate constraints dynamically via the API instead of duplicating columns, unless explicitly requested for cache optimization.



**4. Performance & Integrations**

* **External APIs:** When consuming external geographic APIs (like OpenRouteService or Traccar), use `HttpClientFactory` and implement asynchronous, non-blocking calls.


* **Lazy Evaluation/Caching:** Heavy geospatial calculations (ETA, distance) should follow a lazy-evaluation/on-demand pattern. Always check the cached timestamp (e.g., `last_ping`) and only recalculate if the cache is older than 15 minutes.


* **Tracking Mocking:** If requested to implement tracking/telemetry logic, default to an internal Mock generation if actual Traccar API endpoints are not provided.


**5. How to Respond to User Stories**
When I provide a User Story:

1. Briefly state the technical approach for the specific DDD layers.
2. Generate the **Domain** entity and interface first.
3. Generate the **Application** DTOs (`record`) and Service.
4. Generate the **Infrastructure** EF Core Mapping and Repository implementation.
5. Generate the **API** Controller endpoint.
6. Provide clean, modular, and fully typed C# 10 code. Omit conversational filler; focus strictly on implementation.


**5.1 CRUD Endpoint Playbook (Proven Patterns)**

Concrete conventions distilled from the existing `Auctions`/`RouteSegments` slices. When adding an endpoint, mirror these exactly:

* **Controller:** `[ApiController]`, `[Route("api/<resource-plural>")]` (unversioned — **no `/v1`**), `[Authorize]` on the class and `[Authorize(Policy = Policies.RequireShipperAccess)]` per action. Resolve the tenant with `private Guid GetContractorId() => User.GetCompanyId();` (extension in `Sigloc.Api.Extensions`). Keep actions one-liners delegating to the service. Return `Ok(result)` for reads/updates, `CreatedAtAction(...)` for creates, and `NoContent()` for deletes. Route ids: `{id:guid}`.

* **DTOs:** C# `record`s in `Sigloc.Application/DTOs/<Entity>Dtos.cs`. For partial updates make fields **nullable and optional** (e.g. `record UpdateAuctionRequestDto(string? Name, DateTimeOffset? ExpiresAt)`) and apply only the ones provided in the service.

* **Service (`.Application/Services`):** Depend on repository interfaces + `IUnitOfWork`. Load-scoped-or-throw: `var e = await _repo.GetTrackedByIdAsync(id, contractorId, ct) ?? throw new <Entity>NotFoundException(id);`. Reuse validation helpers (e.g. `ValidateExpiry`) that throw `ValidationException(new[]{ new ValidationError(field, reason) }, message)`. For multi-step writes use `_unitOfWork.ExecuteInTransactionAsync(...)`.

* **Repository (`.Infrastructure/Repositories` + `.Domain/Repositories` interface):** Provide **read** queries with `.AsNoTracking()` and, for mutations, a **tracked** loader (`GetTrackedByIdAsync`) that enforces tenant scope (join to the owning `ConsolidatedRoute`/`Contractor`). Expose `UpdateAsync(entity)` (`_db.<Set>.Update(e); SaveChangesAsync`) and `DeleteAsync(entity)` (`Remove(e); SaveChangesAsync`). Rely on configured cascade deletes rather than manual child cleanup.

* **Errors → HTTP:** Add domain exceptions in `.Application/Exceptions` and map them in `Sigloc.Api/Middlewares/GlobalExceptionMiddleware.cs` (`<Entity>NotFoundException` → 404 `{ error, message }`; `ValidationException` → 400 `{ error:"VALIDATION_ERROR", message, details:[{field,reason}] }`). Never throw raw exceptions to the client.

* **Tenant scoping is mandatory:** every query/mutation must be constrained to the caller's company (directly or via the owning aggregate). A missing row for another tenant must surface as 404, not 403 leakage.

**5.2 Verifying Without a Database**

Docker/Postgres may be unavailable. To smoke-test routing/auth without a DB (Development would try to migrate+seed on boot):

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"; $env:Jwt__SecretKey="<any-long-dev-secret>"
dotnet run --no-build --no-launch-profile --urls "http://localhost:5199"
```

Then confirm the endpoint is registered and protected: the generated `http://localhost:5199/openapi/v1.json` should list the new verbs on the path, and unauthenticated calls should return **401** (route exists) rather than **404** (route missing). Always run `dotnet build Sigloc.slnx` as the primary gate.


**6. Decision Making & Clarifications (CRITICAL)**

Ask Before Assuming: If a User Story is ambiguous, lacks specific requirements, or if you face a decision between multiple valid architectural patterns (e.g., choosing between different LINQ approaches, data structures, or business rules), STOP and ASK.

Present Options: Briefly present the options/trade-offs and wait for my decision before generating the final code. Do not guess the business logic.
