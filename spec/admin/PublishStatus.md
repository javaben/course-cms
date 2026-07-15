# Build Spec for PublishStatus
- database schema: `.\database\admin.sql`

## Summary

`PublishStatus` (發布狀態) is a small lookup / reference table describing the publication
state of content. Its primary key `pkid` is a **user-supplied `tinyint`** (NOT an IDENTITY
column), so it behaves like a natural key — editable when creating, immutable when editing
(mirrors `AppRole.RoleId`, but numeric). It has a `Description` and three boolean flags
(`IsDraft`, `IsPublished`, `IsDiscontinued`). It is referenced as an FK target by
`Course.PublishStatus_pkid` and `Promotion2.PublishStatus_pkid`, so a lookup endpoint is
required, but those child features are not yet implemented (no inbound link buttons wired).

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **tinyint, NOT IDENTITY** (user-supplied `byte`) |
| Foreign Keys | None |
| Required Fields | `Description`, `IsDraft`, `IsPublished`, `IsDiscontinued` (plus `pkid` on create) |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course` (`Course.PublishStatus_pkid`), `Promotion2` (`Promotion2.PublishStatus_pkid`) — features not built yet, links deferred |
| Query Filters | keyword (Description); IsDraft / IsPublished / IsDiscontinued tri-state bool toggles |
| Default Sort | `pkid ASC` |

---

## Localization

### Chinese Table Name

- PublishStatus: 發布狀態
- Description: 內容發布狀態代碼表（草稿／已發布／已停用）

### Chinese Column Names

- pkid: 主代碼
- Description: 狀態說明
- IsDraft: 草稿
- IsPublished: 已發布
- IsDiscontinued: 已停用

---

## Required Fields

Required (NOT NULL):
- `pkid` — tinyint PK, **supplied by the user on create** (no IDENTITY), disabled on edit
- `Description` — nvarchar(50)
- `IsDraft` — bit
- `IsPublished` — bit
- `IsDiscontinued` — bit

Optional (nullable): none.

---

## Foreign Keys

**N/A** — `PublishStatus` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign key columns.

---

## Primary-Foreign Links

The following tables reference `PublishStatus.pkid` as an FK target:

- **Course** (`Course.PublishStatus_pkid`) — 對應課程
- **Promotion2** (`Promotion2.PublishStatus_pkid`) — 對應活動

Neither the Course nor the Promotion feature is implemented yet (only AppRole + this table).
The inbound-navigation link buttons are therefore **deferred** — documented here so they can be
wired when those child list pages exist. No dead links are added to the list/detail pages now.

---

## N-N Relationships

**N/A**

---

## Query Filters

`POST /api/publish-statuses/query` accepts (`PublishStatusQuery`):

- **keyword**: string — LIKE on `Description`.
- **IsDraft**: bool? — tri-state (null = no filter, true = only drafts, false = only non-drafts).
- **IsPublished**: bool? — tri-state exact match on `IsPublished`.
- **IsDiscontinued**: bool? — tri-state exact match on `IsDiscontinued`.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/publish-statuses` | **New** | `{ pkid, description }[]` ordered by `pkid ASC` — for the Course / Promotion FK dropdowns |

`PublishStatus` itself has no FK dropdowns, so it needs no lookups for its own form.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/publish-statuses` | List all (ORDER BY pkid ASC) |
| `POST` | `/api/publish-statuses/query` | Filtered query (body: `PublishStatusQuery`) |
| `GET` | `/api/publish-statuses/{id:int}` | Get by pkid |
| `POST` | `/api/publish-statuses` | Create — **409** if `pkid` already exists |
| `PUT` | `/api/publish-statuses` | Update (pkid from body) — **404** if not found |
| `DELETE` | `/api/publish-statuses/{id:int}` | Delete — **404** if not found |
| `GET` | `/api/lookups/publish-statuses` | Slim lookup list |

No auth attributes (consistent with `AppRolesController`).

---

## Backend Notes

### Models

```csharp
// PublishStatus.cs — response model
public class PublishStatus
{
    public byte Pkid { get; set; }              // 主代碼 (tinyint PK, user-supplied)
    public string Description { get; set; } = ""; // 狀態說明
    public bool IsDraft { get; set; }           // 草稿
    public bool IsPublished { get; set; }       // 已發布
    public bool IsDiscontinued { get; set; }    // 已停用
}

// PublishStatusRequest.cs — write DTO
public class PublishStatusRequest
{
    public byte Pkid { get; set; }              // PK — supplied on create, immutable on edit
    [Required, MaxLength(50)]
    public string Description { get; set; } = "";
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// PublishStatusQuery.cs — search DTO
public class PublishStatusQuery
{
    public string? Keyword { get; set; }
    public bool? IsDraft { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsDiscontinued { get; set; }
}
```

### SQL — SELECT

No FK joins, no `nchar` (Description is `nvarchar`, no `RTRIM`). Shared column list:

```sql
SELECT s.pkid            AS Pkid,
       s.Description     AS Description,
       s.IsDraft         AS IsDraft,
       s.IsPublished     AS IsPublished,
       s.IsDiscontinued  AS IsDiscontinued
FROM PublishStatus s
```

- `GetAllAsync` / `QueryAsync`: append `ORDER BY s.pkid ASC`.
- `QueryAsync` filters: `Description LIKE @Keyword`; `IsDraft = @IsDraft`; `IsPublished = @IsPublished`; `IsDiscontinued = @IsDiscontinued` (each added only when the query field is non-null).
- `GetByIdAsync`: `WHERE s.pkid = @Pkid`.

### SQL — INSERT

`pkid` **is** written (no IDENTITY), no `SCOPE_IDENTITY()`:

```sql
INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
```

### SQL — UPDATE

`pkid` immutable (identifies the row):

```sql
UPDATE PublishStatus
   SET Description    = @Description,
       IsDraft        = @IsDraft,
       IsPublished    = @IsPublished,
       IsDiscontinued = @IsDiscontinued
 WHERE pkid = @Pkid;
```

### Special Column Notes

- `pkid` is `tinyint` → C# `byte`; **not IDENTITY**, so it is part of both INSERT and the request.
- No `nchar`, no `DateOnly`/`TimeOnly`, no computed columns, no FKs, no n-n → no transaction needed
  (single-row writes), matching the simplest repository shape.

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `publish-statuses` | `PublishStatusList` |
| `publish-statuses/new` | `PublishStatusForm` (before `:id`) |
| `publish-statuses/:id/edit` | `PublishStatusForm` |
| `publish-statuses/:id` | `PublishStatusDetail` |

### Angular model

```ts
export interface PublishStatus {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}
export interface PublishStatusRequest { /* same fields */ }
export interface PublishStatusQuery {
  keyword?: string | null;
  isDraft?: boolean | null;
  isPublished?: boolean | null;
  isDiscontinued?: boolean | null;
}
```

`pkid` is numeric — the service does **not** `encodeURIComponent` (unlike string-PK AppRole).

### List component

- Columns: 主代碼 (pkid), 狀態說明 (description), 草稿 (isDraft), 已發布 (isPublished), 已停用 (isDiscontinued), 操作.
- Bool columns render a `p-tag` (是/否) rather than raw booleans.
- Filter drawer: keyword `pInputText`; three tri-state filters via `p-select` (是 / 否 / 不限) for IsDraft/IsPublished/IsDiscontinued.
- Default sort: pkid ASC. Session keys: `publish-status-list-filters` / `-sort` / `-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？`

### Form component

- `pkid` — `p-inputNumber` (`[min]="0" [max]="255"`), required; `.disable()`d in edit mode.
- `description` — `pInputText`, required (maxlength 50).
- `isDraft` / `isPublished` / `isDiscontinued` — `p-checkbox` (`[binary]="true"`).
- On 409, toast: `主代碼「${pkid}」已存在。`

### Sidebar placement

Under existing nav group **系統管理 Admin**, new item: `發布狀態 PublishStatus` (icon `pi pi-flag`),
route `/publish-statuses`.

---

## Files to create / modify

### Backend (`CMS.API`)
- `Models/PublishStatus.cs`, `Models/PublishStatusRequest.cs`, `Models/PublishStatusQuery.cs`
- `Models/PublishStatusLookup.cs`
- `Repositories/IPublishStatusRepository.cs`, `Repositories/PublishStatusRepository.cs`
- `Controllers/PublishStatusesController.cs`
- Modify `Repositories/ILookupRepository.cs` + `LookupRepository.cs` (add `GetPublishStatusesAsync`)
- Modify `Controllers/LookupsController.cs` (add `publish-statuses` route)
- Modify `Program.cs` (register `IPublishStatusRepository`)

### Frontend (`CMS.NG`)
- `core/models/publish-status.model.ts`, `core/models/publish-status-lookup.model.ts`
- `core/services/publish-status.service.ts`
- Modify `core/services/lookup.service.ts` (add `getPublishStatuses`)
- `features/publish-statuses/publish-status-list/` (ts/html/scss)
- `features/publish-statuses/publish-status-detail/` (ts/html/scss)
- `features/publish-statuses/publish-status-form/` (ts/html/scss)
- Modify `app.routes.ts`, `app.ts`, `app.html`

### Tests
- `CMS.API.Tests/Fakes/InMemoryPublishStatusRepository.cs`
- `CMS.API.Tests/PublishStatusesControllerTests.cs`
- `core/services/publish-status.service.spec.ts`
- `features/publish-statuses/*/*.spec.ts` (list / detail / form)
