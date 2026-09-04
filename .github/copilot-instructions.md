# Copilot Instructions for Subly

## Repository snapshot

Subly is a full-stack subscription management app with a Vue 3 frontend, an ASP.NET Core backend, and PostgreSQL. The app is organized around a clean architecture split between domain/application/infrastructure/API layers and a Vite-based frontend with a single Pinia store for app state.

## Build, test, and validation commands

### Backend

```powershell
# restore/build the solution
dotnet restore .\src\backend\Subly.sln
dotnet build .\src\backend\Subly.sln

# run all backend tests
dotnet test .\src\backend\Subly.sln

# run a single backend test by name
# Example: filter by the test class or method name
dotnet test .\src\backend\Subly.sln --filter "FullyQualifiedName~SubscriptionServiceTests"

# start the full local stack through Aspire
dotnet run --project .\src\backend\src\Subly.AppHost\Subly.AppHost.csproj

# manual data reset / seeding
# if needed, these are the repo-supported helpers
dotnet run --project .\src\backend\src\Subly.Api\Subly.Api.csproj -- --seed
dotnet run --project .\src\backend\src\Subly.Api\Subly.Api.csproj -- --reset-db
```

### Frontend

```powershell
cd .\src\frontend
npm ci
npm run build
npm test

# run a single Vitest file
npx vitest run src/tests/<file>.test.ts

# watch mode while iterating
npm run test:watch
```

### Docker / local stack

```powershell
cd .\src
docker compose up --build
docker compose down

# if Docker is unavailable, use podman instead
podman compose up --build
podman compose down
```

There is no dedicated repo lint script in the current frontend package.json or solution setup; build and test commands are the current validation path.

## High-level architecture

```text
Browser
  -> Vue SPA (Vite)
  -> /api proxy
  -> Subly.Api
  -> Subly.Application
  -> ISubscriptionRepository
  -> Subly.Infrastructure (EF Core)
  -> PostgreSQL
```

The important architecture pattern is that the backend is intentionally layered:

- `Subly.Domain`: domain models and invariants. No infrastructure or web dependency.
- `Subly.Application`: use cases, service logic, DTOs, repository abstractions, and `IDateProvider`.
- `Subly.Infrastructure`: EF Core context, repository implementation, seeding, and DI wiring.
- `Subly.Api`: controllers and startup configuration, including JSON serialization and routing.
- `Subly.AppHost`: local orchestration for the backend, database, and frontend when using Aspire.

On the frontend, keep API access centralized through the shared API module and a single Pinia store instead of spreading request logic across components.

## Repository conventions that matter

### Backend conventions

- Domain entities are created through a private constructor and a static `Create(...)` factory with validation.
- `SubscriptionService` depends on `IDateProvider` so date logic is deterministic and easy to test.
- Category validation is centralized in the service using a known lowercase set; do not widen this without updating the same validation path.
- Status updates are performed with `PATCH /api/subscriptions/{id}/status` rather than a full replacement update.
- Validation exceptions from service/domain code are expected to be translated into HTTP 400 responses in the API layer.
- EF Core database initialization is handled at startup; this codebase does not rely on a manual migration workflow for normal local development.
- The connection string key is `sublydb` with `DefaultConnection` as the fallback.

### Testing conventions

- Application tests use inline in-memory test doubles instead of mocks.
- API tests use `CustomWebApplicationFactory` to swap in an EF in-memory database per test instance.
- Test assertions are written with FluentAssertions and xUnit.
- Frontend tests use Vitest + Testing Library; Pinia tests initialize a fresh active Pinia in `beforeEach`.
- Frontend coverage is scoped to `src/app/**/*.{ts,tsx}`.

### Frontend conventions

- All API calls go through the shared `app/api/subscriptionsApi.ts` client and the app’s base `/api` URL.
- The core client state lives in the `useSubscriptionStore` Pinia store, including subscriptions, dashboard summary, loading, and error state.
- The dashboard summary is refreshed after mutation flows (`create`, `updateStatus`, `remove`).
- `initialize()` should fetch subscriptions and summary in parallel and fall back to local summary generation if the API fails.
- Vite is configured to proxy `/api` to the runtime backend URL when launched under Aspire.

## CLI and API parity

This repo includes a CLI project (`src/backend/src/Subly.Cli`) that mirrors API functionality and calls the backend over HTTP. Treat the CLI as a thin client, not as a second implementation of business logic.

When changing an API route or response contract, also update the CLI:

- add a corresponding command under `src/backend/src/Subly.Cli/Commands/`
- add or update API client methods in `src/backend/src/Subly.Cli/Services/`
- mirror DTOs in `src/backend/src/Subly.Cli/Contracts/`
- update the CLI documentation and examples for the command
- validate the endpoint end-to-end through the CLI

## Documentation / library lookup

Before adding code that depends on a library or framework, use Context7 documentation lookup for the relevant technology.

Relevant IDs for this repo:

- .NET: `/dotnet/docs`
- .NET Aspire: `/microsoft/aspire.dev`
- EF Core: `/websites/learn_microsoft_en-us_ef_core`
- Vue.js Guide: `/websites/vuejs_guide`
- Vue.js API: `/websites/vuejs_api`

Use those first when implementing framework-specific code; only resolve a different library ID when needed.

## MCP server configuration

The user-level Copilot CLI configuration already enables the Playwright MCP server:

```text
playwright: npx -y @playwright/mcp@latest
```

Use it for browser-level checks of the Vue frontend when a task requires UI interaction, navigation, screenshots, or network inspection. The server can be inspected with `copilot mcp get playwright` and listed with `copilot mcp list`.

## Working notes for Copilot sessions

- Prefer the existing architecture and project layering over introducing cross-layer shortcuts.
- Keep frontend and backend changes consistent with the existing request/response patterns.
- If a change affects an API contract, review the matching CLI command and documentation together.
- Use repository-provided commands rather than ad hoc scripts when validating changes.

## Existing repo guidance to preserve

The repo already contains an `AGENTS.md` that explicitly captures the Context7 workflow and CLI parity requirements. Keep those rules in force for future sessions.
