# JoeSync

Central data-sync backend for **Jungösterreich Zeitschriftenverlag (JÖZV)**.

JoeSync consolidates data from the relevant source systems (Matomo today; 3CX, HubSpot, MGM CSV,
TYPO3 planned) into a central SQL Server database (`JOEDB_KPI` on `VJOESRV20`) that serves as the
single source of truth for Power BI reporting and the analytics dashboard.

Developed and operated by **Nehl-IT GmbH**.

---

## Background

The previous data processing relied on individual Node.js scripts triggered by the Windows Task
Scheduler. Failures were not logged systematically and often only surfaced once Power BI showed
incomplete or wrong numbers.

JoeSync replaces that fragile setup with a robust, maintainable .NET application featuring complete
logging, delta sync, and a clearly defined database layer.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                       Data sources                          │
│   Matomo API · (3CX · HubSpot · MGM CSV · TYPO3 — planned)  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│             JoeSync.Worker (Windows Service)                 │
│           .NET 10 Worker Service · daily sync                │
│                                                              │
│   ┌──────────────────┐        ┌──────────────────────────┐  │
│   │ JoeSync.Importers│        │      JoeSync.Core        │  │
│   │                  │◄──────►│ ISyncJob · SyncResult    │  │
│   │ MatomoImporter   │        │ JoeSyncDbContext (EF)    │  │
│   │ …Summary/Audio   │        │ SyncLogRepository        │  │
│   └──────────────────┘        └──────────────────────────┘  │
└─────────────────────────────┬────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              JOEDB_KPI · SQL Server 2019 (VJOESRV20)         │
│                                                              │
│   staging.*   Raw data straight from the source systems      │
│   curated.*   Cleaned / enriched tables (stored procedures)  │
│   pbi.*       Views — stable interface for Power BI          │
│   logging.*   Sync log (job, status, runtime, row count)     │
└─────────────────────────────┬────────────────────────────────┘
                              │
                ┌─────────────┴─────────────┐
                ▼                           ▼
   ┌────────────────────────┐   ┌────────────────────────────┐
   │     Power BI reports   │   │   JoeSync.Api + web/        │
   │  (only via pbi.* views)│   │  REST API + React dashboard │
   └────────────────────────┘   └────────────────────────────┘
```

---

## Projects

| Project | Type | Description |
|---|---|---|
| `JoeSync.Core` | Class library | Shared models, `ISyncJob` contract, EF Core `DbContext`, repositories, logging |
| `JoeSync.Importers` | Class library | One importer per data source (Matomo implemented) |
| `JoeSync.Worker` | Worker Service | Background service, scheduling, runs as a Windows Service on VJOESRV20 |
| `JoeSync.Console` | Console app | Local CLI to run individual importers |
| `JoeSync.Api` | ASP.NET Core Web API | Read API over the curated data + manual sync control, documented with Swagger |
| `web/` | Vite + React + TS | Dashboard frontend — **prototype**, not part of the production quality gate |

Test projects live under `tests/` (see [Tests](#tests)).

---

## Data sources

| Source | Type | Importer(s) | Status |
|---|---|---|---|
| Matomo | REST API | `MatomoImporter`, `MatomoVisitsSummaryImporter`, `MatomoMediaAudioImporter` | **Implemented** |
| MGM | CSV files | `MgmCsvImporter` | Planned |
| 3CX | REST API | `ThreeCxImporter` | Planned |
| HubSpot | REST API | `HubSpotImporter` | Planned |
| TYPO3 | MySQL DB | `Typo3Importer` | Planned |

Adding a new importer: implement `ISyncJob` from `JoeSync.Core`, write only to `staging.*`, and
register it in the Worker (and Console) DI container.

---

## Database layout (JOEDB_KPI)

A standalone instance on `VJOESRV20` (SQL Server 2019). The existing `JOEDB_TEST` database is never
touched.

| Schema | Purpose |
|---|---|
| `staging` | Raw data, 1:1 from the source, no transformation. Importers write **here only**. |
| `curated` | Cleaned, enriched tables, populated by stored procedures. |
| `pbi` | Views — the stable Power BI interface. Never written to directly. |
| `logging` | `SyncLog` table (JobName, StartTime, EndTime, Status, RowCount, ErrorMessage). |

Power BI reads **exclusively** through `pbi.*` views, so internal table changes stay transparent to
reports.

---

## Technology

- **.NET 10** — Worker Service, Web API, class libraries
- **SQL Server 2019** — target database on VJOESRV20
- **Entity Framework Core (Code First)** — schema and data access; `SqlBulkCopy` for high-volume
  staging inserts (e.g. Matomo delta sync)
- **Serilog** — structured logging
- **Swashbuckle / OpenAPI** — Swagger documentation for the API
- **xUnit** — unit tests
- **Docker** — local SQL Server for development

---

## Local development

### Prerequisites

- .NET 10 SDK
- Docker Desktop (for a local SQL Server)
- A connection string to a `JOEDB_KPI` database

### Run

```bash
# Local SQL Server via Docker.
# This also creates an empty JOEDB_KPI database, so you can connect right away:
#   Server localhost,1433 · User sa · Password DevPassword123!
# The app then creates the tables via EF Core migrations on first run.
# Reset to a clean slate with:  docker compose down -v
docker compose up -d

# Run a single importer through the CLI
dotnet run --project src/JoeSync.Console -- --job matomo --from 2026-01-01 --to 2026-01-31
dotnet run --project src/JoeSync.Console -- --list

# Run the Worker service locally
dotnet run --project src/JoeSync.Worker

# Run the API (Swagger UI at http://localhost:5080/swagger)
dotnet run --project src/JoeSync.Api

# Run the dashboard frontend (proxies the API)
cd web && npm install && npm run dev   # → http://localhost:5173
```

### Database schema & migrations

EF Core migrations are applied **automatically on startup** by `JoeSync.Worker` and
`JoeSync.Console` (`Database.MigrateAsync()`). `JoeSync.Api` does **not** run migrations — it is a
read-side consumer and expects the schema to already exist, so the schema lifecycle stays owned by a
single process (the Worker in production).

Consequence: after a fresh database (e.g. right after `docker compose up`, where only an empty
`JOEDB_KPI` exists), run the Worker or the Console **once** to create the tables before using the API
on its own:

```bash
# Either of these applies all pending migrations:
dotnet run --project src/JoeSync.Console -- --list   # migrates, lists jobs, exits
dotnet run --project src/JoeSync.Worker              # migrates, then keeps running

# …or apply them directly with the EF CLI:
dotnet ef database update --project src/JoeSync.Core --startup-project src/JoeSync.Worker
```

**Editions dimension seed:** the migrations create the `curated.dim_ausgaben` table but do **not**
fill it. The editions/Schuljahr data is a static dimension seeded by
[`scripts/seed-dim-ausgaben.sql`](scripts/seed-dim-ausgaben.sql) (idempotent, safe to re-run). Run it
**once** after the migrations — otherwise the Schuljahr / Ausgabe filters and the
`/api/dim/ausgaben` · `/schuljahre` endpoints stay empty:

```bash
# Against the Docker SQL Server from above:
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "DevPassword123!" -C -d JOEDB_KPI < scripts/seed-dim-ausgaben.sql
```

### Configuration

`appsettings.json` is committed and contains **no secrets**. Local secrets go into
`appsettings.Development.json` (git-ignored) or user-secrets / environment variables:

```json
{
  "ConnectionStrings": {
    "JoeDB": "Server=localhost,1433;Database=JOEDB_KPI;User Id=sa;Password=…;TrustServerCertificate=True"
  },
  "Matomo": {
    "BaseUrl": "https://stats.example.com",
    "Token": "…"
  }
}
```

> Never commit credentials, tokens, or connection strings with passwords. Production uses
> `appsettings.Production.json` on the server (not in Git) or environment variables.

The connection string can also be supplied via the environment variable `ConnectionStrings__JoeDB`.

---

## API

The API uses ASP.NET Core minimal APIs and is fully documented via Swagger/OpenAPI (available in
every environment at `/swagger`; the root `/` redirects there).

| Method & route | Description |
|---|---|
| `GET /api/daily` · `/summary` | Daily KPI time series and aggregated totals |
| `GET /api/pageviews` · `/timeline` · `/by-edition` | Top pages, daily hits, hits per edition |
| `GET /api/downloads` | Top downloads by file URL |
| `GET /api/media-audio` | Top audio / video resources by play count |
| `GET /api/dim/ausgaben` · `/schuljahre` · `/sites` · `/search-terms` | Dimension lookups |
| `GET /api/brands` · `/{key}/summary` · `/pageviews` · `/downloads` · `/timeline` | Per-brand KPIs |
| `GET /api/sync/jobs` · `/status` · `/log` · `/log/{id}` | Sync inspection |
| `POST /api/sync/run/{jobName}` · `/run-all` | Trigger a sync (202; 409 if one is already running) |
| `GET /health` | Liveness probe |

CORS allows the Vite origins (`http://localhost:5173`, `:4173`) by default; configurable via
`Cors:AllowedOrigins`.

---

## Tests

Pure unit tests (no database required) live under `tests/`:

| Project | Covers |
|---|---|
| `JoeSync.Importers.Tests` | Matomo API client (paging, empty-response edge cases, Actions merge, number-from-string), visit deduplication |
| `JoeSync.Api.Tests` | KPI calculations (`Metrics`: bounce / finish rate), query-filter composition |
| `JoeSync.Worker.Tests` | Scheduler delay calculation |

```bash
dotnet test JoeSync.slnx
```

Database-bound code (SqlBulkCopy staging writers, EF `GROUP BY` aggregation, the `EF.Functions.Like`
search branch) is intentionally excluded from unit tests since it requires an EF provider / SQL
Server.

---

## Continuous integration

GitHub Actions ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs on every push and pull
request to `main` / `dev`:

1. Restore `JoeSync.slnx`
2. Build in `Release` with `-warnaserror` (enforces the "no warnings" convention)
3. Run all unit tests

The job runs on a clean Linux runner because the tests have no SQL Server dependency.

---

## Deploy (VJOESRV20)

`JoeSync.Worker` runs as a **Windows Service**. It runs under `VJOE\service.etl` (same account as the
existing DataLoader) and deploys to `C:\Betrieb\JOEDB_KPI\JoeSync\`.

```powershell
# Publish
dotnet publish src/JoeSync.Worker -c Release -r win-x64 --self-contained -o C:\Betrieb\JOEDB_KPI\JoeSync

# Install the service (first time)
sc.exe create JoeSyncWorker binPath="C:\Betrieb\JOEDB_KPI\JoeSync\JoeSync.Worker.exe" start=auto

# Manage
sc.exe start JoeSyncWorker
sc.exe stop  JoeSyncWorker
sc.exe query JoeSyncWorker
```

---

## Conventions

Enforced via `.editorconfig`:

- `var` everywhere (no explicit types); file-scoped namespaces; braces always required
- `CancellationToken` parameters are always named `cancellationToken`
- Copyright header on every `.cs` file
- Predefined types (`int`, `string`, `bool`), no `this.` qualification
- No warnings — the build (and CI) treats warnings as errors
- New importers implement `ISyncJob`; importers write to `staging.*` only

Branches: `main` (deployed), `dev` (integration), `feature/*` (one per feature/importer). Commit
messages in English, imperative mood.

---

## Maintainer

Development & operations: **Nehl-IT GmbH** — contact@nehl-it.com
Client: Jungösterreich Zeitschriftenverlag GmbH
