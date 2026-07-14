# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A full-stack CMS scaffolded from a SQL Server schema, following a fixed code-generation
convention. New features are added table-by-table, driven by two source-of-truth inputs:

- **`database/*.sql`** — the schema (raw `CREATE TABLE` scripts). `auth.sql` holds `AppRole`,
  `AppUser`, `AppUserRole`; other files (`admin`, `course`, `promotion`) are future features.
- **`spec/code-gen.convention.md`** — the mandatory backend + frontend patterns. Read it before
  generating any new table's code. `spec/sample1.spec.md` is a worked per-feature spec (Course)
  showing the expected depth (FKs, n-n, query filters, copy actions, etc.).
- **`spec/ui-sample-*.png`** — visual style reference only (not content).

Everything lives under `src/`. The only implemented feature so far is **AppRole** (角色) — use it
as the reference implementation for the layering below.

## Commands

Backend (from `src/`, requires .NET 9 SDK):
```powershell
dotnet run --project CMS.API        # http://localhost:5000, Swagger UI at /swagger
dotnet build CMS.slnx
dotnet test                         # all xUnit tests
dotnet test --filter "FullyQualifiedName~AppRolesControllerTests.Create_returns_409"  # single test
```

Frontend (from `src/CMS.NG`, **requires Node 20+**):
```powershell
npm install
npm start                           # ng serve → http://localhost:4200
npm test                            # Karma + Jasmine
npm test -- --include src/app/features/app-roles/app-role-form/app-role-form.spec.ts   # single spec
```

> ⚠️ **Node.js is not installed on this machine.** The Angular project is authored to compile but
> has never been `npm install`/`ng build`-verified here. The backend is fully built and tested.

## Backend architecture (`CMS.API`)

Strict three-layer flow, no Entity Framework — **Dapper only**:

- **Controller** (`Controllers/`) → **`I{Table}Repository`** (`Repositories/`) → **`IDbConnectionFactory`**
  (`Infrastructure/`, singleton, opens `Microsoft.Data.SqlClient` connections per call).
- Repositories are the only place SQL lives. Column lists use exact DB names; `_pkid` FK columns are
  aliased to the C# property in SELECTs (`AS PartnerPkid`). `nchar` columns get `RTRIM()`.
- **String primary keys** (like `AppRole.RoleId`): route is `{id}` with **no `:int` constraint**;
  `PUT` takes the whole record in the body (no id in the route). `AppRole` also has a display-only
  `pkid` IDENTITY column (主代碼) that is *not* the key.
- **N-N relationships** (e.g. `AppUserRole`): synced with **delete-then-reinsert inside a transaction**
  on create/update; read back with a separate query on the same connection. Counts (使用者數) come from
  a correlated subquery in the list SELECT.
- Standard route shape per table: `GET /api/{plural}`, `POST /api/{plural}/query` (filtered),
  `GET|DELETE /api/{plural}/{id}`, `POST|PUT /api/{plural}`, `GET /api/lookups/{plural}` for FK/n-n pickers.
- `Program.cs` registers repositories (scoped), the connection factory (singleton), Swagger, and a
  CORS policy allowing any loopback origin. JSON is default camelCase — C# `PascalCase` becomes
  frontend `camelCase`, including computed getters like `AppUserLookup.Label`.

Tests (`CMS.API.Tests`) exercise **controllers against an in-memory fake repository**
(`Fakes/InMemory*Repository.cs`) — no database needed. The fake must mirror the SQL behaviour
(filtering, n-n sync, ordering) so tests stay meaningful.

## Frontend architecture (`CMS.NG`)

Angular 20 standalone components (no NgModules), PrimeNG for all UI controls.

- **API URL comes from `src/environments/environment*.ts`, never a proxy.** Dev build swaps in
  `environment.development.ts` (`apiBaseUrl: http://localhost:5000`) via `angular.json`
  `fileReplacements`. `apiBaseUrl` is prepended to every request in the services.
- **Path aliases** (`tsconfig.json`): `@env/*`, `@app/*`, `@core/*`, `@features/*`. Use them.
- `core/` holds `models/` (interfaces mirroring the C# DTOs, camelCase) and `services/` (thin
  `HttpClient` wrappers; string ids are `encodeURIComponent`'d).
- Each feature is a **triad** under `features/{table-plural}/`: `{table}-list`, `{table}-detail`,
  `{table}-form` (form is shared by add + edit; PK field is `.disable()`d in edit mode).
- Routing is lazy (`loadComponent`) in `app.routes.ts`; put the specific `:id/edit` and `/new`
  routes **before** the catch-all `:id` route.
- List pages persist state to session storage under keys `{table}-list-filters` /
  `-sort` / `-page`, use a `p-drawer` filter panel and a sortable/paginated `p-table`.
- `App` (`app.ts`/`app.html`) is the sidebar shell; add a new feature's link under the matching nav
  group (AppRole is under 系統管理 Admin). Root template hosts the single `<p-toast>` and
  `<p-confirmdialog>`; `MessageService`/`ConfirmationService` are provided globally in `app.config.ts`.

## Run order for local dev

1. Create the `CMS` database on `.\SQLEXPRESS` and run `database/*.sql` (start with `auth.sql`).
2. `dotnet run --project CMS.API` (port 5000).
3. `npm start` in `CMS.NG` (port 4200).

## Keeping this file current

Update CLAUDE.md in the same change whenever you alter something it describes — a new table
feature added, a command/port/path-alias change, or a deviation from the patterns above. When a new
convention emerges that isn't yet in `spec/code-gen.convention.md`, note it here and flag the gap.
