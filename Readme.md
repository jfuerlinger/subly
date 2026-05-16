# Subly

Subly ist eine Full-Stack-Anwendung zur Verwaltung von Abonnements.  
Die App bietet ein Dashboard mit Kennzahlen (monatlich/jährlich/anstehende Zahlungen) sowie CRUD-Funktionen für Abos inkl. Statuswechsel (aktiv, pausiert, gekündigt).

## Tech-Stack

- **Frontend:** Vue 3, TypeScript, Vite, Pinia, Vue Router
- **Backend:** ASP.NET Core 9, Clean-Architecture-ähnliche Schichten (Api/Application/Domain/Infrastructure)
- **Datenbank:** PostgreSQL mit Entity Framework Core (Npgsql)
- **Orchestrierung (lokal):** .NET Aspire AppHost

## Architektur

### Gesamtübersicht

```mermaid
flowchart LR
    Browser[Browser] --> Frontend[Vue SPA (Vite)]
    Frontend -->|REST /api| Api[Subly.Api]
    Api --> Application[Subly.Application<br/>SubscriptionService]
    Application --> Repository[ISubscriptionRepository]
    Repository --> Infrastructure[Subly.Infrastructure<br/>EfSubscriptionRepository]
    Infrastructure --> Db[(PostgreSQL)]
    AppHost[Subly.AppHost (Aspire)] -. startet/verdrahtet .-> Frontend
    AppHost -. startet/verdrahtet .-> Api
    AppHost -. startet/verdrahtet .-> Db
```

### Backend-Schichten und Abhängigkeiten

```mermaid
flowchart TB
    Api[Subly.Api]
    App[Subly.Application]
    Infra[Subly.Infrastructure]
    Domain[Subly.Domain]
    Db[(PostgreSQL)]

    Api --> App
    Api --> Infra
    App --> Domain
    Infra --> App
    Infra --> Domain
    Infra --> Db
```

### Request-Flow (Beispiel)

```mermaid
sequenceDiagram
    participant U as Benutzer
    participant F as Frontend (Vue)
    participant A as API (Subly.Api)
    participant S as Service (SubscriptionService)
    participant R as Repository (EF)
    participant P as PostgreSQL

    U->>F: Abo anlegen
    F->>A: POST /api/subscriptions
    A->>S: CreateSubscriptionAsync(...)
    S->>R: AddAsync + SaveChangesAsync
    R->>P: INSERT subscription
    P-->>R: OK
    R-->>S: OK
    S-->>A: SubscriptionDto
    A-->>F: 201 Created
```

## Projektstruktur

```text
src/
├─ backend/
│  ├─ src/
│  │  ├─ Subly.AppHost          # Aspire-Orchestrierung
│  │  ├─ Subly.Api              # REST API (Controller, Program)
│  │  ├─ Subly.Application      # Use-Cases, Services, Contracts
│  │  ├─ Subly.Domain           # Domain-Modelle
│  │  ├─ Subly.Infrastructure   # EF Core, Repository, Migrationen, Seeding
│  │  └─ Subly.ServiceDefaults  # Service Defaults/Observability
│  └─ tests/
│     ├─ Subly.Api.Tests
│     └─ Subly.Application.Tests
└─ frontend/
   ├─ src/                      # Vue App
   └─ Dockerfile
```

## Projekt starten

### Option 1 (empfohlen): Mit .NET Aspire

Voraussetzungen:

- .NET SDK 9
- Node.js 22+
- Docker (für PostgreSQL-Container)

1. Frontend-Abhängigkeiten installieren:

   ```powershell
   cd .\src\frontend
   npm ci
   cd ..\..
   ```

2. Gesamtsystem über AppHost starten:

   ```powershell
   dotnet run --project .\src\backend\src\Subly.AppHost\Subly.AppHost.csproj
   ```

3. Im Aspire-Dashboard die Endpunkte für **frontend** und **api** öffnen.

---

### Option 2: Mit Docker Compose

Diese Variante baut und startet **PostgreSQL**, **Backend** und **Frontend** als Container.

```powershell
cd .\src
docker compose up --build
```

Danach ist das Frontend unter `http://localhost:4173` erreichbar und die API über den Frontend-Proxy unter `/api`.

Stoppen:

```powershell
docker compose down
```

## Nützliche Kommandos

Backend-Tests:

```powershell
dotnet test .\src\backend\Subly.sln
```

Frontend-Tests:

```powershell
cd .\src\frontend
npm test
```
