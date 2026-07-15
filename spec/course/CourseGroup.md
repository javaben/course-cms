# Build Spec for CourseGroup
- database schema: `.\database\course.sql`

## Summary

`CourseGroup` (課程群組) is a minimal reference entity — an `smallint` IDENTITY primary key plus a
single required `Description`. It has no foreign keys. It is referenced as an FK target by
`Course.CourseGroup_pkid` (nullable) and `PartnerCourseGroup.CourseGroup_pkid`, so a
`course-groups` lookup endpoint is required; those child features are not built yet, so inbound
link buttons are deferred. Shape-wise it is the same IDENTITY-PK pattern as `Partner`, only smaller.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (auto-generated `short`) |
| Foreign Keys | None |
| Required Fields | `Description` |
| N-N Relationships | N/A (`PartnerCourseGroup` carries its own payload → child entity, not a junction picker) |
| Primary-Foreign Links | `Course` (`CourseGroup_pkid`, nullable), `PartnerCourseGroup` (`CourseGroup_pkid`) — features not built yet, links deferred |
| Query Filters | keyword (Description) |
| Default Sort | `pkid ASC` |

---

## Localization

### Chinese Table Name

- CourseGroup: 課程群組
- Description: 課程群組分類

### Chinese Column Names

- pkid: 主代碼
- Description: 群組說明

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):
- `Description` — nvarchar(100)

Optional (nullable): none.

`pkid` is `smallint` IDENTITY — auto-generated, never entered by the user.

---

## Foreign Keys

**N/A** — `CourseGroup` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign key columns.

---

## Primary-Foreign Links

The following tables reference `CourseGroup.pkid` as an FK target:

- **Course** (`Course.CourseGroup_pkid`, nullable) — 對應課程
- **PartnerCourseGroup** (`PartnerCourseGroup.CourseGroup_pkid`) — 對應廠商課程群組

Neither child feature is implemented yet (only AppRole, PublishStatus, Partner, and this table).
Inbound-navigation link buttons are therefore **deferred** — documented here for when those child
list pages exist. No dead links are added now.

---

## N-N Relationships

**N/A**

`PartnerCourseGroup` links Partner ↔ CourseGroup but carries its own `pkid` IDENTITY plus
`DisplayOrder` and `Description` payload columns, so it is a first-class child entity, **not** a
pure junction table to be managed with a multi-select picker.

---

## Query Filters

`POST /api/course-groups/query` accepts (`CourseGroupQuery`):

- **keyword**: string — LIKE on `Description`.

No FK dropdowns, bool toggles, or date ranges apply to this table.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/course-groups` | **New** | `{ pkid, description }[]` ordered by `pkid ASC` — for the Course / PartnerCourseGroup FK dropdowns |

`CourseGroup` itself has no FK dropdowns, so its own form needs no lookups.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/course-groups` | List all (ORDER BY pkid ASC) |
| `POST` | `/api/course-groups/query` | Filtered query (body: `CourseGroupQuery`) |
| `GET` | `/api/course-groups/{id:int}` | Get by pkid |
| `POST` | `/api/course-groups` | Create — pkid auto-generated (`SCOPE_IDENTITY`) |
| `PUT` | `/api/course-groups` | Update (pkid from body) — **404** if not found |
| `DELETE` | `/api/course-groups/{id:int}` | Delete — **404** if not found |
| `GET` | `/api/lookups/course-groups` | Slim lookup list |

No 409 path on create — `pkid` is IDENTITY and there is no other UNIQUE constraint.
No auth attributes (consistent with the other controllers).

---

## Backend Notes

### Models

```csharp
// CourseGroup.cs — response model
public class CourseGroup
{
    public short Pkid { get; set; }               // 主代碼 (smallint IDENTITY)
    public string Description { get; set; } = ""; // 群組說明
}

// CourseGroupRequest.cs — write DTO
public class CourseGroupRequest
{
    public short Pkid { get; set; }               // identifies the row on UPDATE; ignored on INSERT
    [Required, MaxLength(100)]
    public string Description { get; set; } = "";
}

// CourseGroupQuery.cs — search DTO
public class CourseGroupQuery
{
    public string? Keyword { get; set; }
}
```

### SQL — SELECT

No FK joins, no `nchar` (`Description` is `nvarchar`, no `RTRIM`):

```sql
SELECT g.pkid         AS Pkid,
       g.Description  AS Description
FROM CourseGroup g
```

- `GetAllAsync` / `QueryAsync`: append `ORDER BY g.pkid ASC`.
- `QueryAsync` keyword filter (added only when non-empty): `g.Description LIKE @Keyword`.
- `GetByIdAsync`: `WHERE g.pkid = @Pkid`.

### SQL — INSERT

`pkid` excluded (IDENTITY); returns the new id:

```sql
INSERT INTO CourseGroup (Description) VALUES (@Description);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

### SQL — UPDATE

`pkid` immutable (identifies the row):

```sql
UPDATE CourseGroup SET Description = @Description WHERE pkid = @Pkid;
```

### Special Column Notes

- `pkid` is `smallint` IDENTITY → C# `short`; excluded from INSERT, read back via `SCOPE_IDENTITY()`.
- No `nchar`, no `DateOnly`/`TimeOnly`, no computed columns, no FKs, no n-n → simplest repository shape.

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `course-groups` | `CourseGroupList` |
| `course-groups/new` | `CourseGroupForm` (before `:id`) |
| `course-groups/:id/edit` | `CourseGroupForm` |
| `course-groups/:id` | `CourseGroupDetail` |

### Angular model

```ts
export interface CourseGroup {
  pkid: number;
  description: string;
}
export interface CourseGroupRequest { pkid: number; description: string; }
export interface CourseGroupQuery { keyword?: string | null; }
```

`pkid` is numeric — the service does **not** `encodeURIComponent`.

### List component

- Columns: 主代碼 (pkid), 群組說明 (description), 操作.
- Filter drawer: single keyword `pInputText`.
- Default sort: pkid ASC. Session keys: `course-group-list-filters` / `-sort` / `-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？`

### Form component

- `pkid` is IDENTITY → **no input field**; carried internally (0 on create, loaded value on edit),
  sent in the request for update. In edit mode the pkid is shown read-only in the header.
- `description` — `pInputText`, required (maxlength 100).

### Sidebar placement

Existing nav group **課程管理 Course** — add item `課程群組 CourseGroup` (icon `pi pi-sitemap`),
route `/course-groups`, after the existing 合作廠商 Partner item.

---

## Files to create / modify

### Backend (`CMS.API`)
- `Models/CourseGroup.cs`, `CourseGroupRequest.cs`, `CourseGroupQuery.cs`, `CourseGroupLookup.cs`
- `Repositories/ICourseGroupRepository.cs`, `CourseGroupRepository.cs`
- `Controllers/CourseGroupsController.cs`
- Modify `Repositories/ILookupRepository.cs` + `LookupRepository.cs` (add `GetCourseGroupsAsync`)
- Modify `Controllers/LookupsController.cs` (add `course-groups` route)
- Modify `Program.cs` (register `ICourseGroupRepository`)

### Frontend (`CMS.NG`)
- `core/models/course-group.model.ts`, `course-group-lookup.model.ts`
- `core/services/course-group.service.ts`
- Modify `core/services/lookup.service.ts` (add `getCourseGroups`)
- `features/course-groups/course-group-list/` (ts/html/scss)
- `features/course-groups/course-group-detail/` (ts/html/scss)
- `features/course-groups/course-group-form/` (ts/html/scss)
- Modify `app.routes.ts`, `app.ts` (Course nav group)

### Tests
- `CMS.API.Tests/Fakes/InMemoryCourseGroupRepository.cs`
- `CMS.API.Tests/CourseGroupsControllerTests.cs`
- `core/services/course-group.service.spec.ts`
- `features/course-groups/*/*.spec.ts` (list / detail / form)
