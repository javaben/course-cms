# Build Spec for Partner
- database schema: `.\database\course.sql`

## Summary

`Partner` (合作廠商) is a standalone reference entity representing a course provider / partner
brand. It has an **`smallint` IDENTITY** primary key (auto-generated — contrast with
`AppRole`'s string PK and `PublishStatus`'s user-supplied `tinyint`), a `Name`, a short `AppKey`
code, two display-name variants, a `DisplayOrder`, and an optional `ImageFilename`. It has **no
foreign keys**. It is referenced as an FK target by `Certification`, `Course`, and
`PartnerCourseGroup`, so a `partners` lookup endpoint is required; those child features are not
built yet, so inbound link buttons are deferred.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (auto-generated `short`) |
| Foreign Keys | None |
| Required Fields | `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`, `DisplayOrder` |
| N-N Relationships | N/A (`PartnerCourseGroup` carries its own payload → child entity, not a junction picker) |
| Primary-Foreign Links | `Certification` (`Partner_pkid`), `Course` (`Partner_pkid`), `PartnerCourseGroup` (`Partner_pkid`) — features not built yet, links deferred |
| Query Filters | keyword (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage) |
| Default Sort | `DisplayOrder ASC, pkid ASC` |

---

## Localization

### Chinese Table Name

- Partner: 合作廠商
- Description: 課程合作廠商 / 品牌主資料

### Chinese Column Names

- pkid: 主代碼
- Name: 廠商名稱
- AppKey: 應用代碼
- NameOnPartnerMenu: 選單顯示名稱
- NameOnCourseDetailPage: 課程頁顯示名稱
- DisplayOrder: 顯示順序
- ImageFilename: 圖片檔名

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):
- `Name` — nvarchar(50)
- `AppKey` — varchar(10)
- `NameOnPartnerMenu` — nvarchar(200)
- `NameOnCourseDetailPage` — nvarchar(50)
- `DisplayOrder` — int

Optional (nullable):
- `ImageFilename` — varchar(50)

`pkid` is `smallint` IDENTITY — auto-generated, never entered by the user.

---

## Foreign Keys

**N/A** — `Partner` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign key columns.

---

## Primary-Foreign Links

The following tables reference `Partner.pkid` as an FK target:

- **Certification** (`Certification.Partner_pkid`) — 對應認證
- **Course** (`Course.Partner_pkid`) — 對應課程
- **PartnerCourseGroup** (`PartnerCourseGroup.Partner_pkid`) — 對應廠商課程群組

None of these child features are implemented yet (only AppRole, PublishStatus, and this table).
Inbound-navigation link buttons are therefore **deferred** — documented here for when those child
list pages exist. No dead links are added now.

---

## N-N Relationships

**N/A**

`PartnerCourseGroup` links Partner ↔ CourseGroup but carries its own `pkid` IDENTITY plus
`DisplayOrder` and `Description` payload columns, so it is a first-class child entity (a future
Primary-Foreign link), **not** a pure junction table to be managed with a multi-select picker.

---

## Query Filters

`POST /api/partners/query` accepts (`PartnerQuery`):

- **keyword**: string — LIKE across `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`.
  (`ImageFilename` excluded — it is a filename, not an identifying field.)

No FK dropdowns, bool toggles, or date ranges apply to this table.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **New** | `{ pkid, name }[]` ordered by `DisplayOrder ASC` — for the Certification / Course / PartnerCourseGroup FK dropdowns |

`Partner` itself has no FK dropdowns, so its own form needs no lookups.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/partners` | List all (ORDER BY DisplayOrder ASC, pkid ASC) |
| `POST` | `/api/partners/query` | Filtered query (body: `PartnerQuery`) |
| `GET` | `/api/partners/{id:int}` | Get by pkid |
| `POST` | `/api/partners` | Create — pkid auto-generated (`SCOPE_IDENTITY`) |
| `PUT` | `/api/partners` | Update (pkid from body) — **404** if not found |
| `DELETE` | `/api/partners/{id:int}` | Delete — **404** if not found |
| `GET` | `/api/lookups/partners` | Slim lookup list |

No 409 path on create — `pkid` is IDENTITY and there is no other UNIQUE constraint.
No auth attributes (consistent with `AppRolesController` / `PublishStatusesController`).

---

## Backend Notes

### Models

```csharp
// Partner.cs — response model
public class Partner
{
    public short Pkid { get; set; }                       // 主代碼 (smallint IDENTITY)
    public string Name { get; set; } = "";                // 廠商名稱
    public string AppKey { get; set; } = "";              // 應用代碼
    public string NameOnPartnerMenu { get; set; } = "";   // 選單顯示名稱
    public string NameOnCourseDetailPage { get; set; } = ""; // 課程頁顯示名稱
    public int DisplayOrder { get; set; }                 // 顯示順序
    public string? ImageFilename { get; set; }            // 圖片檔名
}

// PartnerRequest.cs — write DTO
public class PartnerRequest
{
    public short Pkid { get; set; }                       // identifies the row on UPDATE; ignored on INSERT
    [Required, MaxLength(50)]  public string Name { get; set; } = "";
    [Required, MaxLength(10)]  public string AppKey { get; set; } = "";
    [Required, MaxLength(200)] public string NameOnPartnerMenu { get; set; } = "";
    [Required, MaxLength(50)]  public string NameOnCourseDetailPage { get; set; } = "";
    public int DisplayOrder { get; set; }
    [MaxLength(50)]            public string? ImageFilename { get; set; }
}

// PartnerQuery.cs — search DTO
public class PartnerQuery
{
    public string? Keyword { get; set; }
}
```

### SQL — SELECT

No FK joins, no `nchar` (`Name`/`AppKey` etc. are `nvarchar`/`varchar`, no `RTRIM`):

```sql
SELECT p.pkid                    AS Pkid,
       p.Name                    AS Name,
       p.AppKey                  AS AppKey,
       p.NameOnPartnerMenu       AS NameOnPartnerMenu,
       p.NameOnCourseDetailPage  AS NameOnCourseDetailPage,
       p.DisplayOrder            AS DisplayOrder,
       p.ImageFilename           AS ImageFilename
FROM Partner p
```

- `GetAllAsync` / `QueryAsync`: append `ORDER BY p.DisplayOrder ASC, p.pkid ASC`.
- `QueryAsync` keyword filter (added only when non-empty):
  `(p.Name LIKE @Keyword OR p.AppKey LIKE @Keyword OR p.NameOnPartnerMenu LIKE @Keyword OR p.NameOnCourseDetailPage LIKE @Keyword)`.
- `GetByIdAsync`: `WHERE p.pkid = @Pkid`.

### SQL — INSERT

`pkid` excluded (IDENTITY); returns the new id:

```sql
INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

### SQL — UPDATE

`pkid` immutable (identifies the row):

```sql
UPDATE Partner
   SET Name                   = @Name,
       AppKey                 = @AppKey,
       NameOnPartnerMenu      = @NameOnPartnerMenu,
       NameOnCourseDetailPage = @NameOnCourseDetailPage,
       DisplayOrder           = @DisplayOrder,
       ImageFilename          = @ImageFilename
 WHERE pkid = @Pkid;
```

### Special Column Notes

- `pkid` is `smallint` IDENTITY → C# `short`; excluded from INSERT, read back via `SCOPE_IDENTITY()`.
- No `nchar`, no `DateOnly`/`TimeOnly`, no computed columns, no FKs, no n-n → simplest repository shape
  (single-row writes, no transaction).

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `partners` | `PartnerList` |
| `partners/new` | `PartnerForm` (before `:id`) |
| `partners/:id/edit` | `PartnerForm` |
| `partners/:id` | `PartnerDetail` |

### Angular model

```ts
export interface Partner {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename?: string | null;
}
export interface PartnerRequest { /* same fields; pkid used on update */ }
export interface PartnerQuery { keyword?: string | null; }
```

`pkid` is numeric — the service does **not** `encodeURIComponent`.

### List component

- Columns: 主代碼 (pkid), 廠商名稱 (name), 應用代碼 (appKey), 選單顯示名稱 (nameOnPartnerMenu),
  課程頁顯示名稱 (nameOnCourseDetailPage), 顯示順序 (displayOrder), 操作.
- Filter drawer: single keyword `pInputText`.
- Default sort: displayOrder ASC. Session keys: `partner-list-filters` / `-sort` / `-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.name}」？`

### Form component

- `pkid` is IDENTITY → **no input field**; carried internally (0 on create, loaded value on edit),
  sent in the request for update. In edit mode the pkid is shown read-only in the header.
- `name` — `pInputText`, required (maxlength 50).
- `appKey` — `pInputText`, required (maxlength 10).
- `nameOnPartnerMenu` — `pInputText`, required (maxlength 200).
- `nameOnCourseDetailPage` — `pInputText`, required (maxlength 50).
- `displayOrder` — `p-inputNumber`, required (`[useGrouping]="false"`, `[min]="0"`).
- `imageFilename` — `pInputText`, optional (maxlength 50).

### Sidebar placement

New nav group **課程管理 Course** (currently empty in `app.ts`) — add item
`合作廠商 Partner` (icon `pi pi-building`), route `/partners`, and expand the group.

---

## Files to create / modify

### Backend (`CMS.API`)
- `Models/Partner.cs`, `PartnerRequest.cs`, `PartnerQuery.cs`, `PartnerLookup.cs`
- `Repositories/IPartnerRepository.cs`, `PartnerRepository.cs`
- `Controllers/PartnersController.cs`
- Modify `Repositories/ILookupRepository.cs` + `LookupRepository.cs` (add `GetPartnersAsync`)
- Modify `Controllers/LookupsController.cs` (add `partners` route)
- Modify `Program.cs` (register `IPartnerRepository`)

### Frontend (`CMS.NG`)
- `core/models/partner.model.ts`, `partner-lookup.model.ts`
- `core/services/partner.service.ts`
- Modify `core/services/lookup.service.ts` (add `getPartners`)
- `features/partners/partner-list/` (ts/html/scss)
- `features/partners/partner-detail/` (ts/html/scss)
- `features/partners/partner-form/` (ts/html/scss)
- Modify `app.routes.ts`, `app.ts` (Course nav group)

### Tests
- `CMS.API.Tests/Fakes/InMemoryPartnerRepository.cs`
- `CMS.API.Tests/PartnersControllerTests.cs`
- `core/services/partner.service.spec.ts`
- `features/partners/*/*.spec.ts` (list / detail / form)
