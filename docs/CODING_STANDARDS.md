# HappyBoxx Coding Standards

> Status: **Living document**. Changes go through a pull request and need approval from at least one maintainer.
>
> **MUST** / **MUST NOT** = mandatory. **SHOULD** / **SHOULD NOT** = strong default; deviations need a reason in the PR.

---

## 1. General principles

1. **Simple first.** Build the simplest thing that works. Add an abstraction only when there is a second real use case (YAGNI).
2. **Consistency over preference.** Follow the existing pattern in a service, even if you'd do it differently. Change patterns in a separate PR.
3. **Readable code.** Use intention-revealing names. Write comments for *why*, not *what*.
4. **Secure by default.** Validate every input at the boundary. Never trust the client. Never log secrets or personal data (see §9).
5. **Every change is tested.** Bug fixes come with a regression test.
6. **Automate the rules.** Analyzers, `.editorconfig`, ESLint and Prettier enforce these rules. The build fails on warnings (`TreatWarningsAsErrors`).

---

## 2. Repository layout

```
HappyBoxx.slnx
Directory.Build.props        # shared MSBuild settings (TFM, nullable, analyzers)
Directory.Packages.props     # Central Package Management – ALL NuGet versions live here
global.json                  # pinned .NET SDK
docs/                        # architecture & standards
src/
  Aspire/
    HappyBoxx.AppHost/       # .NET Aspire orchestrator (local dev)
    HappyBoxx.ServiceDefaults/ # OpenTelemetry, health checks, resilience, service discovery
  BuildingBlocks/
    HappyBoxx.BuildingBlocks/  # small shared kernel: Result, Error, endpoints, validation, paging
  Gateway/
    HappyBoxx.Gateway/       # YARP API gateway – single entry point for the front-ends
  Services/
    <Service>/HappyBoxx.<Service>.Api/   # one deployable per bounded context
  Web/
    admin-portal/            # React + TypeScript admin SPA
tests/
  HappyBoxx.<Service>.UnitTests/
```

### 2.1 Microservice rules

- Each service owns **one bounded context** (e.g. Catalog, Inventory) and **its own database**. A service **MUST NOT** read or write another service's database.
- Services talk to each other over **HTTP via service discovery** (`https+http://catalog-api`) or, later, **asynchronous integration events** through a message broker. Prefer events for state changes.
- `BuildingBlocks` holds **technical** concerns only. It **MUST NOT** contain domain models or business rules from any service.
- Front-ends **MUST** call services **only through the Gateway** (`/api/v1/...`).
- A service **MUST** be deployable on its own. Avoid shared release trains.

---

## 3. Backend (C# / .NET)

### 3.1 Platform

| Concern            | Choice                                                     |
|--------------------|------------------------------------------------------------|
| Runtime            | .NET 10 (LTS), C# latest                                   |
| API style          | ASP.NET Core **Minimal APIs**, `TypedResults`              |
| Orchestration      | .NET Aspire (local dev, telemetry dashboard)               |
| Data access        | EF Core 10 + SQL Server, one `DbContext` per service       |
| Validation         | FluentValidation                                           |
| Errors             | `Result` pattern + RFC 9457 **ProblemDetails**             |
| API docs           | `Microsoft.AspNetCore.OpenApi` + Scalar UI (dev only)      |
| Gateway            | YARP                                                       |
| Observability      | OpenTelemetry (logs, traces, metrics) via ServiceDefaults  |
| Testing            | xUnit, Shouldly, EF Core InMemory (unit), Aspire testing (integration) |

We don't use MediatR or AutoMapper. Handlers are plain classes injected into endpoints, and mapping is explicit (`ToResponse()` methods). This keeps the code easy to follow and avoids licensing and "magic" issues.

### 3.2 Service structure – Vertical Slices

Code is organised **by feature**, not by technical layer:

```
HappyBoxx.Catalog.Api/
  Domain/                      # entities, enums, domain errors – no ASP.NET / EF dependencies
    Categories/Category.cs
    Categories/CategoryErrors.cs
  Features/                    # one file per use case
    Categories/
      CategoryResponse.cs
      CreateCategory.cs        # request + validator + handler + endpoint
      GetCategoryById.cs
  Infrastructure/
    Persistence/
      CatalogDbContext.cs
      Configurations/CategoryConfiguration.cs
      Migrations/
  Program.cs
```

A feature file **MAY** contain several small types for one use case: request/response records, the validator, the handler and the endpoint. Name the types so they're unique within the service, e.g. `CreateCategoryRequest`, `CreateCategoryValidator`, `CreateCategoryHandler`, `CreateCategoryEndpoint`. Unique names keep OpenAPI schema names clean.

### 3.3 Naming & style

- Follow the [.NET naming guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/naming-guidelines) and the repository `.editorconfig`.
- `PascalCase` for types, methods, properties and constants. `camelCase` for locals and parameters. `_camelCase` for private fields.
- Interfaces start with `I`. Async methods end with `Async`.
- Use **file-scoped namespaces** that match the folder path.
- One public top-level type per file, except feature slices (see §3.2).
- Use `var` when the type is obvious from the right-hand side.
- Use **primary constructors** for DI-only classes (handlers, services).
- Classes are `sealed` by default. Types are `internal` unless something outside the assembly needs them.
- Use `record` for DTOs, requests and responses. They are immutable.
- **Nullable reference types are on.** Don't use the `!` null-forgiving operator without a comment explaining why.
- No magic numbers or strings. Use constants, for example `Category.NameMaxLength`.
- Use `TimeProvider` for anything time-related. Never use `DateTime.Now`. Store times as `DateTimeOffset` in UTC with the `...Utc` suffix.
- IDs are `Guid` v7 (`Guid.CreateVersion7()`). They sort by time, which keeps indexes efficient.

### 3.4 Domain model

- Entities protect their own invariants. Use **private setters**, **factory methods** (`Create(...)`) and **behaviour methods** (`Update(...)`, `AdjustQuantity(...)`). Don't expose public setters.
- Entities have a private parameterless constructor for EF Core only.
- Expected business failures (not found, conflict, rule violated) return a `Result`/`Error`. **Don't throw exceptions for control flow.**
- Exceptions are for truly exceptional or programming errors (`ArgumentException.ThrowIfNullOrWhiteSpace`, etc.).
- Each aggregate has an `XxxErrors` static class with **stable error codes**, e.g. `Category.NotFound`.

### 3.5 API design

- Base path: `/api/v{major}/{resource}`, for example `/api/v1/categories`. Resources are **plural, kebab-case nouns**.
- HTTP semantics:
  - `GET` – read, never changes state. Returns `200`, or `404` if not found.
  - `POST` – create. Returns `201 Created` with a `Location` header.
  - `PUT` – full update. Returns `200` with the resource, or `204`.
  - `DELETE` – avoid hard deletes of business data. Prefer `IsActive = false`.
- Errors are **always** `application/problem+json` (ProblemDetails) and include `traceId` and `errorCode` where they apply.
  - `400` validation (`ValidationProblem` with field errors), `404` not found, `409` conflict or concurrency, `500` unexpected. Never leak stack traces.
- Lists are **always paginated**: `?page=1&pageSize=20`, with `pageSize` capped at 100. The response is `PagedResult<T>` (`items`, `page`, `pageSize`, `totalCount`, `totalPages`).
- JSON uses `camelCase`. Enums are serialised as strings.
- Every endpoint has a `.WithName()`, `.WithTags()`, a summary, and its documented response types (`Produces...`).
- Every endpoint that accepts input **MUST** add `.WithRequestValidation<TRequest>()`.
- Every async method accepts and passes on a `CancellationToken`.

### 3.6 Data access (EF Core)

- One `DbContext` per service, registered with `AddDbContext` plus `EnrichSqlServerDbContext` for Aspire retries, health checks and telemetry.
- Mapping lives in `IEntityTypeConfiguration<T>` classes, not attributes. Always set max lengths, decimal precision and indexes.
- Read queries **MUST** use `AsNoTracking()` and project to response DTOs (`Select`). Don't return entities from APIs.
- Prefer one `SaveChangesAsync` per request. Don't use a generic repository on top of EF Core.
- Use **optimistic concurrency** (`rowversion`) on entities that are updated concurrently (stock).
- Schema changes go **only** through EF Core migrations, one migration per change, with a descriptive name:
  ```
  dotnet ef migrations add AddProductSku -p src/Services/Catalog/HappyBoxx.Catalog.Api -o Infrastructure/Persistence/Migrations
  ```
- Migrations run automatically **only in Development**. Other environments use an idempotent script or bundle in the CD pipeline.
- Never build SQL by concatenating strings. Use LINQ or parameterised `FromSql`.

### 3.7 Logging & observability

- Use `ILogger<T>` with **structured message templates**: `logger.LogInformation("Stock adjusted for {ProductId}", id)`. Never use string interpolation in log calls.
- Use `[LoggerMessage]` source-generated logging on hot paths.
- Log levels: `Information` for business events, `Warning` for recoverable problems, `Error` for failed operations with the exception attached.
- **MUST NOT** log secrets, tokens, passwords, connection strings or personal data such as buyer names, phone numbers or addresses.
- Tracing, metrics and health checks (`/health`, `/alive`) come from `ServiceDefaults`. Every service calls `AddServiceDefaults()` and `MapDefaultEndpoints()`.

### 3.8 Configuration & secrets

- Use the strongly typed **Options pattern** (`IOptions<T>`) with `ValidateDataAnnotations().ValidateOnStart()`.
- **No secrets in source control.** Use `dotnet user-secrets` locally and Azure Key Vault (or similar) in deployed environments.
- Connection strings use the names `catalogdb` and `inventorydb`. Aspire injects them.

### 3.9 Async & performance

- Use async all the way. **Never** use `.Result`, `.Wait()` or `async void`, except for event handlers.
- Don't use `ConfigureAwait(false)` in ASP.NET Core application code.
- Return `IReadOnlyList<T>` or `IReadOnlyCollection<T>` from methods. Accept the narrowest type you need.
- Use `IHttpClientFactory` or typed clients. Never `new HttpClient()`.

---

## 4. Frontend (React / TypeScript)

### 4.1 Platform

| Concern       | Choice                                         |
|---------------|------------------------------------------------|
| Build         | Vite                                           |
| Language      | TypeScript, `strict: true`                     |
| Routing       | React Router                                   |
| Server state  | TanStack Query (no server data in global stores) |
| HTTP          | Thin `fetch` wrapper (`src/lib/api/httpClient.ts`) |
| Lint / format | ESLint (flat config) + Prettier                |
| Testing       | Vitest + React Testing Library                 |
| UI kit        | *To be decided*                                |

### 4.2 Structure (feature-based)

```
src/
  app/          # App shell: providers, router
  components/   # shared, generic UI components (no business logic)
  features/
    categories/
      api.ts        # API calls + TanStack Query hooks
      types.ts      # DTO types matching the backend contracts
      pages/        # route-level components
      components/   # feature-specific components
  lib/          # framework-agnostic helpers (http client, formatting)
  pages/        # generic pages (Dashboard, NotFound)
```

- A feature **MUST NOT** import from another feature's internals. Shared code moves to `components/` or `lib/`.
- Use the `@/` path alias for imports from `src`.

### 4.3 Conventions

- Function components and hooks only. No class components.
- File names: `PascalCase.tsx` for components and `camelCase.ts` for everything else.
- Name exports. Avoid default exports, except where a tool requires them.
- Don't use `any`. Use `unknown` and narrow it. Model backend DTOs as TypeScript types in `types.ts`.
- All server calls go through TanStack Query hooks (`useXxxQuery`, `useXxxMutation`) with **query key factories**. Never call `fetch` directly from components.
- Keep components small. Move logic into hooks. Derive state instead of duplicating it.
- Handle all three states for every query: loading, error (ProblemDetails) and empty.
- Accessibility: use semantic HTML, give every input a label, make everything keyboard-navigable, and use `alt` text on images.
- Don't use `dangerouslySetInnerHTML`. Never store tokens in `localStorage`.

---

## 5. Testing

- **Unit tests** cover domain logic, validators and handlers. Name them `Method_Scenario_ExpectedResult`.
- Use the Arrange / Act / Assert structure with one logical assertion per test, and Shouldly for assertions.
- **Integration tests** (later) use `Aspire.Hosting.Testing` or `WebApplicationFactory` against a real SQL Server.
- Tests are deterministic. Use a fake `TimeProvider` and don't rely on test order.
- Coverage target: 80% or more on `Domain/` and `Features/`. Treat coverage as a signal, not a goal.
- Frontend: test behaviour, not implementation details. Query elements by role or label.

---

## 6. Git workflow

- Use **trunk-based development**. `main` is always releasable.
- Branch names: `feature/<ticket>-short-desc`, `fix/<ticket>-short-desc`, `chore/...`.
- Use **Conventional Commits**: `feat(catalog): add product search`, `fix(inventory): prevent negative stock`.
- Keep PRs small, ideally under 400 changed lines, with at least one approval and a green CI. Squash-merge.
- PR checklist:
  - [ ] Build passes with zero warnings
  - [ ] Tests added or updated
  - [ ] Migration added if the schema changed
  - [ ] No secrets or personal data in code or logs
  - [ ] OpenAPI metadata (name, tags, response types) present
  - [ ] Docs updated if behaviour or architecture changed

---

## 7. Dependencies

- All NuGet versions live in `Directory.Packages.props`. Never put `Version=` in a `.csproj`.
- Before adding a dependency, check its license, maintenance activity and known vulnerabilities, and agree on it in the PR.
- Run `dotnet list package --vulnerable` and `npm audit` in CI.

---

## 8. Definition of Done

A change is done when it is merged to `main`, CI is green, it's covered by tests, it has OpenAPI docs, it can be observed (logs and traces), and it has been reviewed.

---

## 9. Security checklist (OWASP-aligned)

- Validate all input on the server, even if the UI already validates it.
- Authentication and authorization (admin vs buyer) will be enforced **at every service** and at the gateway once added. Never rely on the UI hiding buttons.
- Use parameterised queries only (EF Core).
- HTTPS everywhere, with HSTS in non-development environments.
- Use specific CORS origins, never `*` with credentials.
- Error responses don't reveal internals.
- Keep dependencies patched and monitor vulnerable packages.
- Apply rate limiting at the gateway (planned).
