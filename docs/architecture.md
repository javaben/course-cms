# Architecture reference

Deep detail for the backend/frontend layering. Read when generating a new table or
changing the layering. The mandatory per-table patterns live in
`spec/code-gen.convention.md`; this file explains the *why* and the PK-shape variations.

## Backend (`CMS.API`)

Strict three-layer flow, no Entity Framework — **Dapper only**:

- **Controller** (`Controllers/`) → **`I{Table}Repository`** (`Repositories/`) → **`IDbConnectionFactory`**
  (`Infrastructure/`, singleton, opens `Microsoft.Data.SqlClient` connections per call).
- Repositories are the only place SQL lives. Column lists use exact DB names; `_pkid` FK columns are
  aliased to the C# property in SELECTs (`AS PartnerPkid`). `nchar` columns get `RTRIM()`.
- `Program.cs` registers repositories (scoped), the connection factory (singleton), Swagger, and a
  CORS policy allowing any loopback origin. JSON is default camelCase — C# `PascalCase` becomes
  frontend `camelCase`, including computed getters like `AppUserLookup.Label`.

### The three PK shapes

- **String primary keys** (like `AppRole.RoleId`): route is `{id}` with **no `:int` constraint**;
  `PUT` takes the whole record in the body (no id in the route). `AppRole` also has a display-only
  `pkid` IDENTITY column (主代碼) that is *not* the key.
- **Numeric natural keys** (like `PublishStatus.pkid`, a `tinyint`/`byte` that is **NOT IDENTITY**):
  supplied by the client on create (checked for 409 via `ExistsAsync`), included in INSERT (no
  `SCOPE_IDENTITY()`), immutable on edit; route uses `{id:int}`; the frontend form `.disable()`s the
  pkid field in edit mode and the service does **not** `encodeURIComponent` it.
- **Numeric IDENTITY keys** (like `Partner.pkid`, a `smallint` IDENTITY): auto-generated — excluded
  from INSERT and read back via `SELECT CAST(SCOPE_IDENTITY() AS smallint)`; no 409/`ExistsAsync` on
  create; `PUT` takes pkid from the body; route `{id:int}`. The frontend form has **no pkid input**
  at all — it's carried internally (0 on create, loaded value on edit) and the new pkid is read from
  the create response for navigation.

### N-N relationships

`AppUserRole` and similar: synced with **delete-then-reinsert inside a transaction** on
create/update; read back with a separate query on the same connection. Counts (使用者數) come from a
correlated subquery in the list SELECT.

### Routes

Standard route shape per table: `GET /api/{plural}`, `POST /api/{plural}/query` (filtered),
`GET|DELETE /api/{plural}/{id}`, `POST|PUT /api/{plural}`, `GET /api/lookups/{plural}` for FK/n-n pickers.

### Tests

`CMS.API.Tests` exercise **controllers against an in-memory fake repository**
(`Fakes/InMemory*Repository.cs`) — no database needed. The fake must mirror the SQL behaviour
(filtering, n-n sync, ordering) so tests stay meaningful.

## Frontend (`CMS.NG`)

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
