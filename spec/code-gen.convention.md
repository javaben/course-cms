# Code Generation Patterns

## Backend

  ### Models
  - `{TABLE}.cs` — response model (with nav objects for FKs, subquery counts for n-n)
  - `{TABLE}Request.cs` — write DTO (FK pkids only; n-n as `List<int>`)
  - `{TABLE}Query.cs` — search DTO (Keyword?, FK pkids?, bool fields?, date ranges?)

  ### Repository
  - `I{TABLE}Repository.cs` + `{TABLE}Repository.cs`
  - Dapper only (no EF). Multi-map when JOINing FK nav objects: alias each nav's leading column
    `AS Pkid` and pass `splitOn: "Pkid,Pkid,..."` (one entry per nav) — the repeated split name maps
    each nav's own pkid and Dapper hands back a null nav when a LEFT-JOINed key is null. First used by
    Course (Partner + CourseGroup + PublishStatus).
  - `nchar` columns: always `RTRIM()` in SQL
  - n-n: delete-then-reinsert on update; separate query on same connection for read

  ### Controller
  - **Auth (app-wide, since the Auth feature):** a global fallback authorization policy in `Program.cs`
    requires an authenticated user on **every** endpoint. Generated controllers need **no** `[Authorize]`
    attribute — they are protected automatically. Only add `[AllowAnonymous]` (per-action) for endpoints
    that must be public. The Angular services need no change: the HTTP interceptor attaches the bearer
    token to every request. (Full detail in `docs/features.md` → Auth.)
  - Route: `/api/{tablePlural}`; `PUT` takes pkid from body (no route param)
  - String PKs: route `{id}` (no `:int` constraint); service uses `encodeURIComponent`
  - `date`/`time` columns: use C# `DateOnly`/`TimeOnly`. Dapper 2.1.66 reads them fine but **cannot
    bind them as parameters** without a handler — `Infrastructure/DapperTypeHandlers.cs` provides
    `DateOnlyTypeHandler`/`TimeOnlyTypeHandler`, registered via `SqlMapper.AddTypeHandler` at the top of
    `Program.cs`. System.Text.Json serialises `DateOnly` as `"yyyy-MM-dd"`. First used by
    FeaturedPromoItem (`ScheduleOn`); verified live against SQL Server.
  
 ## Frontend

  ### Files
  - `features/{table-plural}/{table}-list/`
  - `features/{table-plural}/{table}-detail/`
  - `features/{table-plural}/{table}-form/`

  ### List page
  - Session storage keys: `{table}-list-filters`, `{table}-list-sort`, `{table}-list-page`
  - Sortable/paginated `p-table`; filter drawer (`p-drawer`)
  - **Inline cell-editing** (opt-in; first used by Course): PrimeNG editors in the `p-table` body,
    activated by `(dblclick)` **only** (single-click must not edit). Bind one shared `editValue` +
    an `editing` signal `{ pkid, field }`; commit on the editor's blur/change. Validate client-side
    first (required non-empty, numbers ≥ 0, valid dates, cross-field rules) — on failure show an
    inline error and **stay in edit mode**; on success fetch the full record (`getById`) to preserve
    N-N lists, apply the one field, PUT, and **revert on server error** (don't mutate the row until
    the save resolves). Read-only columns (PK, FK-label columns) simply omit the `(dblclick)` editor.
  - `p-select` in drawer: always `appendTo="body"`; mapped `{ pkid, label }[]` getter; `[filter]="true"` for 10+ options
  - `p-select` / `p-multiselect` with 100+ items: add `[virtualScroll]="true" [virtualScrollItemSize]="43"`

  ### Form page
  - Reactive Forms; `forkJoin` for parallel lookup calls on init
  - `p-datepicker`: convert ISO string ↔ `Date` on load/save
  - `p-datepicker [timeOnly]="true"` for `time` columns; `parseTime`/`toTimeStr` helpers
  - `p-multiselect` for n-n: `[maxSelectedLabels]="9999"`; wrap chips via `::ng-deep`

  ### Sidebar nav
  - Add entry under the appropriate nav group in `app.html` / `app.ts`

  ### QR code (opt-in; first used by Course detail)
  - Standalone component (`<app-{table}-qr-code [pkid] [id]>`) rendered inside a detail card.
  - Generate a **PNG data URL** with the `qrcode` package (`QRCode.toDataURL(url)`); display via
    `<img [src]>`, download by synthesizing an `<a download>` — no server round-trip.
  - `qrcode` is CommonJS → add it to `angular.json` `allowedCommonJsDependencies`.
  
 
  ## Special Types

  | Column type | Handling |
  |-------------|---------|
  | `nchar(n)` | `RTRIM()` in all SQL SELECTs |
  | `time(7)` | C# `TimeOnly` via `TimeOnlyTypeHandler` (registered in Program.cs); display with `\| slice:0:5`; `p-datepicker [timeOnly]` in form |
  | `date` | C# `DateOnly` via `DateOnlyTypeHandler` (registered in Program.cs); JSON `"yyyy-MM-dd"`; `p-datepicker` in form (or ISO-string grid for FeaturedPromoItem) |
  | `smallint` PK | No special handling |
  | `nvarchar` PK (string) | Controller route `{id}` (no `:int`); service calls `encodeURIComponent(id)` | 
  
  
   ## API Endpoints

  | Method | Route | Description |
  |--------|-------|-------------|
  | GET    | `/api/{plural}` | All records |
  | POST   | `/api/{plural}/query` | Filtered search |
  | GET    | `/api/{plural}/{id}` | Single record |
  | POST   | `/api/{plural}` | Create |
  | PUT    | `/api/{plural}` | Update (pkid in body) |
  | DELETE | `/api/{plural}/{id}` | Delete |
  | GET    | `/api/lookups/{plural}` | Slim lookup list (if used as FK target) |