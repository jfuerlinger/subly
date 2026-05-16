# Copilot Instructions — Subly

## Project Overview

Subly is a full-stack subscription management app.

- **Frontend:** Vue 3 + TypeScript + Vite + Pinia + Vue Router
- **Backend:** ASP.NET Core 9, Clean Architecture (Domain / Application / Infrastructure / Api)
- **Database:** PostgreSQL via Entity Framework Core (Npgsql)
- **Orchestration:** .NET Aspire AppHost (dev), Docker Compose (containerised)

## Commands

### Backend

```powershell
# Run all backend tests
dotnet test .\src\backend\Subly.sln

# Run a single test by name
dotnet test .\src\backend\Subly.sln --filter "FullyQualifiedName~<TestName>"

# Start the full system (Aspire)
dotnet run --project .\src\backend\src\Subly.AppHost\Subly.AppHost.csproj

# Seed / reset the database manually
dotnet run --project .\src\backend\src\Subly.Api\Subly.Api.csproj -- --seed
dotnet run --project .\src\backend\src\Subly.Api\Subly.Api.csproj -- --reset-db
```

### Frontend

```powershell
cd .\src\frontend
npm ci            # install dependencies
npm test          # run all tests (vitest, single pass)
npm run test:watch  # vitest watch mode
npx vitest run src/tests/<file>.test.ts  # run a single test file
npm run build     # type-check + Vite production build
```

### Docker

```powershell
cd .\src
docker compose up --build   # builds and starts everything
docker compose down
```

## Architecture

```
Browser → Vue SPA (/api proxy) → Subly.Api → Subly.Application → ISubscriptionRepository
                                                                         ↓
                                                             Subly.Infrastructure (EF Core)
                                                                         ↓
                                                                    PostgreSQL
```

**Layer responsibilities:**

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `Subly.Domain` | `Subscription` entity, `BillingCycle`/`SubscriptionStatus` enums. No dependencies. |
| Application | `Subly.Application` | `SubscriptionService`, contracts (DTOs/requests), `ISubscriptionRepository`, `IDateProvider` abstractions |
| Infrastructure | `Subly.Infrastructure` | EF Core `SublyDbContext`, `EfSubscriptionRepository`, seeder, `DependencyInjection.cs` |
| API | `Subly.Api` | Controllers, `Program.cs`, wires everything via DI |
| Frontend | `src/frontend` | Vue 3 SPA; all API calls through `app/api/subscriptionsApi.ts`; single Pinia store `useSubscriptionStore` |

## Key Conventions

### Backend

- **Domain entities use a private constructor + static `Create()` factory** with validation. All properties have private setters. Example: `Subscription.Create(...)`.
- **`IDateProvider`** is injected into `SubscriptionService` for testable date logic. `SystemDateProvider` wraps `DateOnly.FromDateTime(DateTime.Today)` in production; tests use an inline `FixedDateProvider`.
- **Categories** are validated against a hardcoded `HashSet<string>` (lowercase) in `SubscriptionService.KnownCategories`. Valid values: `streaming`, `software`, `insurance`, `telecom`, `energy`, `fitness`, `news`, `cloud`, `membership`.
- **Enums are serialised as camelCase strings** in JSON (configured globally in `Program.cs` via `JsonStringEnumConverter`).
- **Updating subscription status** uses `PATCH /api/subscriptions/{id}/status`, not a full PUT.
- **ArgumentException** thrown in the service/domain layer is caught in the controller and returned as `400 ValidationProblem`.
- **Connection string key** is `sublydb`; fallback is `DefaultConnection`. Aspire wires this automatically.
- **DB migrations run on startup** via `EnsureDatabaseInitializedAsync`; no manual CLI migration step needed in dev.

### Backend Testing

- **Application unit tests** (`Subly.Application.Tests`): use inline `InMemorySubscriptionRepository` and `FixedDateProvider` test doubles — no mocking framework.
- **API integration tests** (`Subly.Api.Tests`): use `CustomWebApplicationFactory` which replaces `SublyDbContext` with an in-memory EF database (unique name per factory instance).
- Assertions use **FluentAssertions**; test framework is **xUnit**.

### Frontend

- **All API calls** go through `app/api/subscriptionsApi.ts` using the shared `apiClient` (axios, base URL `/api`).
- **Single store** `useSubscriptionStore` (Pinia Composition API style) holds subscriptions, dashboard summary, loading, and error state. Summary is refreshed after every mutation (`create`, `updateStatus`, `remove`).
- **`initialize()`** fetches subscriptions and summary in parallel (`Promise.all`); on error, falls back to local computation via `buildDashboardSummary`.
- **Frontend tests** (Vitest + `@testing-library/vue`) mock API modules with `vi.spyOn`. Store tests use `setActivePinia(createPinia())` in `beforeEach`.
- Coverage is collected only for `src/app/**/*.{ts,tsx}`.
- When running under Aspire, the API URL is injected via `services__api__https__0` / `services__api__http__0` env vars; Vite proxies `/api` to that target.

## Documentation Lookup (Context7)

Always use Context7 MCP before implementing features that rely on an external library. Pre-resolved IDs:

| Technology | Context7 Library ID |
|---|---|
| .NET | `/dotnet/docs` |
| .NET Aspire | `/microsoft/aspire.dev` |
| Entity Framework Core | `/websites/learn_microsoft_en-us_ef_core` |
| Vue.js (guide) | `/websites/vuejs_guide` |
| Vue.js (API ref) | `/websites/vuejs_api` |

Workflow: check the table above → if listed, call `context7:query-docs` directly; otherwise resolve with `context7:resolve-library-id` first.

## General Conventions

- Use metric/European units in documentation and comments.
- Write commit messages in English.
- Prefer clear, readable code over clever one-liners.
