# Implemented features

All built via `/crud`; together they cover the three PK shapes (see `docs/architecture.md`).
Use **AppRole** as the reference implementation.

| Feature | 中文 | Nav group | PK shape | Notes |
|---------|------|-----------|----------|-------|
| AppRole | 角色 | 系統管理 Admin | string PK + display-only `pkid` IDENTITY | primary reference; n-n to AppUser |
| AppUser | 使用者 | 系統管理 Admin | string PK `UserId` + display-only `pkid` IDENTITY | near-mirror of AppRole; n-n to AppRole via `AppUserRole`; password handling below |
| PublishStatus | 發布狀態 | — | numeric natural PK (`tinyint`, NOT IDENTITY) | lookup table, bool flags |
| Partner | 合作廠商 | 課程管理 Course | `smallint` IDENTITY PK | standalone entity |
| CourseGroup | 課程群組 | 課程管理 Course | `smallint` IDENTITY PK | minimal single-column case |
| Course | 課程 | 課程管理 Course | `int` IDENTITY PK | **first FK multi-map + N-N feature**; QR code on detail; **inline cell-editing on the list**; see below |
| FeaturedPromoItem | 上稿作業 | 首頁管理 Home | `int` IDENTITY PK | **custom board** (not the triad); FKs to TrainingCenter + Promotion2; see below |
| Auth | 登入/個人資料 | — | n/a (no table) | JWT login + **global bearer authorization** + self-profile update; frontend login/guard/interceptor/role-gated menu; see below |

## AppUser password handling

A convention **not yet in `spec/code-gen.convention.md`** — flag when generalizing.

- `PasswordHash` (`nvarchar(800)`) is **backend-only** — excluded from `AppUserRequest`, every
  SELECT, and all Angular models (never sent to or from the client).
- On **create** the repository reads `SysConfig.configValue WHERE configKey='appConfig'` (a JSON
  object), extracts `defaultPassword`, hashes it via `PasswordHasher.Hash` (PBKDF2, fresh salt), and
  stores it (with `PasswordUpdatedTime = UtcNow`). Each call salts afresh, so users sharing the default
  password do not share a stored hash.
- **Update** never touches the hash.
- `POST /api/app-users/{id}/reset-password` re-hashes the default and bumps `PasswordUpdatedTime`.
  **The AppUser *edit form's* reset button now calls the Admin-gated `POST /api/Auth/reset-password`
  instead** (visible only to Admins; see the Auth feature). Password hashes also change via the Auth
  self-service `POST /api/Auth/change-password`.
- `PasswordUpdatedTime` is `datetime` NULL, read-only, displayed with the `+ 'Z'` UTC fix.
- The `app-roles` lookup (`GET /api/lookups/app-roles`) feeds the AppUser role picker.

## Auth / Login (登入)

A **non-CRUD** feature — no table, no triad. `AuthController` uses `[controller]` → `api/Auth` (the one
controller that isn't a lowercase-plural route) and hosts login, JWT issuing, self-profile update,
change-password, and Admin reset-password. **Full spec: `spec/auth.md`.**

### Login + JWT issuing — `POST /api/Auth/login`

Takes `{ userId, password }`, returns `{ userId, userName, accessToken }` (a `LoginResponse`). Marked
`[AllowAnonymous]` (per-action, not on the class — see authorization below).

- **Credential check** (`AuthRepository.FindByUserIdAsync`, Dapper): UserId match **and** `IsActive = 1`
  **and** `PasswordHasher.Verify(PasswordHash, password) != Failed`. Any failure returns a single generic
  `401 { message: "invalid credentials" }` — the controller evaluates all three then collapses to one
  result so the caller can't tell which failed. `PasswordHash` is read only inside `AuthUser` (a
  server-side credential model) and never serialized — `LoginResponse` has no hash field.
- **Password hashing** is shared: `Infrastructure/PasswordHasher` is the single source of truth —
  `Hash` (PBKDF2-HMAC-SHA256, per-user random salt, 210k iterations) for every write, `Verify`
  (constant-time) for every check — used by both `AuthController` and `AppUserRepository`, so a password
  set by one verifies under the other. Hashes are salted, so they are never compared directly. Legacy
  unsalted-SHA-256 rows still verify and are upgraded in place on their owner's next login (`Verify`
  returns `SuccessRehashNeeded` → `AuthRepository.UpgradePasswordHashAsync`); nothing writes that format
  any more.
- **JWT** (`System.IdentityModel.Tokens.Jwt`, HMAC-SHA256): the signing secret is `SysConfig['appConfig']
  .symmetricSecurityKey`, read at runtime — never hard-coded — via `Infrastructure/SigningKeyProvider`
  (`ISigningKeyProvider`, singleton, caches after first DB read). The **same provider** signs (issue) and
  validates (bearer), so a token this app mints is a token it accepts. Claims: `sub`/`userId` = UserId,
  `userName` = UserName, and one **`role`** claim per RoleId from `AppUserRole` (short claim name, decoded
  client-side; `RoleClaimType`/`NameClaimType` set on validation). 24h lifetime
  (`AuthController.TokenLifetime`). Built via the `JwtSecurityToken` constructor so claim types are
  verbatim (no in/outbound remapping; `MapInboundClaims = false`).

### Authorization (global) — secure by default

`Program.cs` wires `AddAuthentication().AddJwtBearer()` (options bound to `ISigningKeyProvider` via
`Configure<ISigningKeyProvider>` so the key is resolved from DI at runtime) + `app.UseAuthentication()`
before `app.UseAuthorization()`. A **fallback authorization policy** (`RequireAuthenticatedUser`) applies
to every endpoint that doesn't opt out, so any request without a valid bearer token gets 401. Only
`AuthController.Login` carries `[AllowAnonymous]`; `AuthController` has **no class-level `[AllowAnonymous]`**
(that would leak onto `/profile`).

### My Profile — `PUT /api/Auth/profile` (`[Authorize]`)

Updates **only** `UserName`, for the UserId taken **from the JWT** (`User.FindFirstValue("userId")`) — never
from the body. `UpdateProfileRequest` carries only `UserName` (required, trimmed; blank → 400); UserId/roles
are unchangeable here. Returns `ProfileResponse { userId, userName }` (canonical, trimmed) via
`AuthRepository.UpdateUserNameAsync`.

### Change password (self) — `POST /api/Auth/change-password` (`[Authorize]`)

UserId from the JWT. Order: (1) `PasswordHasher.Verify(storedHash, currentPassword)` must not be `Failed`
else `400 目前密碼不正確`; (2) `PasswordPolicy.IsComplexEnough(newPassword)` (len ≥ 8 AND ≥ 3 of
upper/lower/digit/symbol) else `400` with the exact bilingual complexity message; (3) new == confirm else
`400`; (4) store `PasswordHasher.Hash(new)` + bump `PasswordUpdatedTime`, `204`. `ChangePasswordRequest`
is all-plaintext; no hash in or out.

### Reset password to default (Admin) — `POST /api/Auth/reset-password` (`[Authorize(Roles="Admin")]`)

**Role-enforced server-side** — a non-Admin authenticated caller gets **403**. Body is just `{ userId }`.
Reads `appConfig.defaultPassword` at runtime, sets `PasswordHash = PasswordHasher.Hash(default)` + `PasswordUpdatedTime`,
`204` (404 if user missing). The AppUser edit form's reset button (Admin-only, `auth.isAdmin`) calls this;
the client sends only the UserId. (The older `POST /api/app-users/{id}/reset-password` still exists but the
form now uses the Admin-gated Auth endpoint.)

Shared helpers: `Infrastructure/AppConfig.GetRequiredProperty` (one place to read the `appConfig` JSON;
used by `SigningKeyProvider` and the reset flow) and `Infrastructure/PasswordPolicy`.

### Tests

- `AuthControllerTests` (unit, `Fakes/InMemoryAuthRepository` + `FakeSigningKeyProvider`): login happy path,
  generic 401s, decoded `role` claims, ~24h expiry, no hash leak; profile updates the JWT user only,
  leaves roles untouched, rejects blank UserName, 401 without a `userId` claim.
- `AuthorizationTests` (integration, `WebApplicationFactory<Program>` + `ConfigureTestServices` swapping the
  key provider + two repos): protected endpoint 401 without / with garbage token, 200 with a real token,
  login reachable anonymously, `/profile` requires auth + ignores body `userId` + 400 on blank name.

### Frontend (`CMS.NG`) auth

- **`core/services/auth.service.ts`** — `login`/`logout`/`updateUserName`; stores the profile in **session
  storage** (key `auth-profile`, not local storage); `profile`/`roles`/`isAdmin` signals; decodes the JWT
  `role` claim (base64url) — no separate roles call.
- **`core/interceptors/auth.interceptor.ts`** (functional, in `provideHttpClient(withInterceptors(...))`) —
  attaches `Authorization: Bearer <token>` on every request; on a 401 clears the session and redirects to
  `/login` (exempts the login request so its error surfaces on the page).
- **`core/guards/auth.guard.ts`** (`canActivateChild`) wraps every app route; `/login` is the only public
  route. `/profile` is a guarded child.
- **`features/auth/login`** and **`features/auth/profile`** (UserId + roles read-only, UserName editable).
- **`features/auth/password-policy.ts`** — shared client validators (`isPasswordComplex`,
  `passwordComplexityValidator`, `passwordsMatchValidator`) + the bilingual complexity message; mirrors
  the backend `PasswordPolicy`. Used by the Change Password form on the profile page.
- **App shell** (`app.ts`/`app.html`): chrome renders only when authenticated; header shows the UserName +
  a **My Profile** link + **Logout**; the **系統管理 Admin** nav group is hidden unless roles include
  `Admin` (`visibleGroups` computed).
- Tests: `auth.service.spec`, `auth.interceptor.spec`, `auth.guard.spec`, `app.spec` (Admin-menu
  visibility), `login.spec`, `profile.spec` (read-only fields, save, change-password client validation),
  `password-policy.spec`, and `app-user-form.spec` (reset button Admin-only).

## Course (課程)

The first standard-triad feature with **outbound FK nav joins** and **N-N junctions**, plus two
extras beyond the generated spec (`spec/course/Course.md`): a QR code on the detail page and
inline cell-editing on the list.

- **FK multi-map read**: `CourseRepository.SelectColumns` LEFT JOINs `Partner`, `CourseGroup`
  (nullable), `PublishStatus`; each nav's leading column is aliased `AS Pkid` and read with
  `Query<Course, PartnerLookup, CourseGroupLookup, PublishStatusLookup, Course>(..., splitOn:
  "Pkid,Pkid,Pkid")`. The repeated-`Pkid` split maps each nav's own pkid too (verified live: a
  course returns all three nav objects with labels; a null `CourseGroup_pkid` yields a null nav).
- **N-N junctions** `CourseInCertification` and `CourseJobCategories`: id lists populated on
  get-by-id only, re-synced via delete-then-reinsert inside the create/update transaction. The
  list/query reads do **not** carry the id lists — so the inline-edit save fetches the full record
  first (see below) to avoid wiping them.
- **Two new lookups** in `LookupsController`: `GET /api/lookups/certifications`
  (`Partner.Name + ' - ' + RTRIM(Certification.Title)`, Title is `nchar` → RTRIM) and
  `GET /api/lookups/job-categories`.
- **`date` columns** `ScheduleOn`/`ScheduleOff` use `DateOnly` (the registered `DateOnlyTypeHandler`);
  the form serializes dates with **local** components (`course-form/course-date.util.ts` `toIso`),
  never `toISOString()`, and defaults `ScheduleOff` to `ScheduleOn + 10 years` on change.

### QR code on the detail page (基本資料)

- `course-detail/course-qr-code/` is a standalone component (`<app-course-qr-code [pkid] [courseId]>`)
  embedded in the 基本資料 card. It encodes
  `https://www.uuu.com.tw/Course/Show/{pkid}/{courseId}`, shows the `CourseId` as the caption, and
  renders the QR as a **PNG data URL** via the `qrcode` package (`QRCode.toDataURL`) — so the
  download (`{courseId}.png`, via a synthesized `<a download>`) needs no server round-trip.
- `qrcode` is CommonJS → allow-listed in `angular.json` `allowedCommonJsDependencies`.
- The base URL comes from `environment.publicSiteBaseUrl` (not a hardcoded literal) so the on-screen
  QR and the server flyer's QR share one source — see the PDF flyer below.

### PDF flyer download (課程宣傳單) — `GET /api/courses/{id:int}/pdf`

A server-rendered, bilingual (繁體中文 + English) one/two-page flyer, downloadable from the detail page.

- **Endpoint**: thin action on `CoursesController` — `GetByIdAsync` → `404` if null → resolve cert
  labels (`ILookupRepository.GetCertificationsAsync`, filtered by the course's `CertificationPkids`)
  → `File(bytes, "application/pdf", "course-{id}.pdf")`. No new course SQL; reuses the detail data
  path. Inherits the global JWT policy — **no `[Authorize]`, no `[AllowAnonymous]`**.
- **`ICoursePdfService` / `CoursePdfService`** (`src/CMS.API/Services/`): pure layout → `byte[]`, so
  it is unit-testable without HTTP. Built with **QuestPDF** (fluent API; `LicenseType.Community` — a
  personal/learning build, see the feature plan). Layout: kicker/title/subtitle type scale, a labeled
  bilingual meta row with units (`學費 NT$12,000` · `時數 40 小時` · `學分 3.5`; `0`/null price →
  `免費 Free`, never `NT$0`), fixed section order Objective → Target → Outline → Certifications with
  **empty sections hidden**, and long Outline paginating cleanly to page 2.
- **Stored HTML is flattened first** (`ToPlainText`): the curated free-text columns are legacy
  `nvarchar(max)` holding markup — the detail page renders it, but QuestPDF draws a string verbatim,
  so raw `<br>` / `<span style=…>` (and, in ~10 rows, a whole pasted `<!DOCTYPE html>` document with
  its CSS) would print on the sheet. `script`/`style`/`head` are dropped whole so their content can't
  leak, block tags become newlines to keep line structure, remaining tags are stripped, and entities
  decode **last** (so an encoded `&lt;b&gt;` can't re-enter as a live tag). A body that flattens to
  nothing hides its section instead of printing a bare heading. ~18% of courses carry such markup.
- **CJK font**: Noto Sans TC (SIL OFL) **Regular + Bold static** faces are embedded via
  `<EmbeddedResource>` (`Assets/Fonts/`, ~5.8 MB each) and registered with QuestPDF's `FontManager`
  in the service's static ctor (also sets the license) — so glyphs render on machines without the
  font installed. **A variable font won't work** (QuestPDF can't select a Bold instance from a VF).
- **Server QR** (`QRCoder` `PngByteQRCode` — avoids the Windows-only `System.Drawing.Common`): encodes
  the exact same string as the on-page QR, `{PublicSite:BaseUrl}/Course/Show/{pkid}/{courseId}`
  (base in `appsettings.json`). Rendered as a fail-safe block: QR image + caption + the URL printed
  as readable text, so the sheet still works if a scan fails.
- **Frontend**: `CourseService.downloadPdf` uses `HttpClient` with `responseType:'blob'` so the auth
  interceptor attaches the bearer (a plain `<a href>` would 401). The detail-page button shows a
  loading state (disabled + label swap, blocks double-submit), a success toast, and on failure
  **decodes the blob error body** (`await err.error.text()`, else it renders as `[object Blob]`) with
  a status-branched message (401/404/500) and a ~15 s client timeout.
- **Deploy**: Windows/IIS — SkiaSharp (QuestPDF's renderer) works out of the box. A Linux/container
  move would need `SkiaSharp.NativeAssets.Linux` + `libfontconfig1` and a re-run of the font check.
- **Tests**: `CoursePdfServiceTests` (valid `%PDF`, **font embedded** via `NotoSansTC` in the bytes,
  null/all-empty no-throw, long-Outline multi-page via `/MediaBox` count, price formatting, QR URL
  shape, `ToPlainText` markup flattening); controller tests (`application/pdf` File + `404`); E2E
  `AuthorizationTests` (`401`/`200`).
  One thing tests **cannot** prove — that glyphs render, not □□□ — is a mandatory manual gate before
  merge: open a generated PDF on a machine with Noto Sans TC uninstalled.

### Inline cell-editing on the list (課程 list)

- PrimeNG editors inside the `p-table` body, activated by **double-click** (`(dblclick)` only —
  single-click never edits). Every column is editable **except** the three read-only ones:
  主代碼 (`pkid`), 原廠 (`partner`), 課程群組 (`courseGroup`). Editor per type: `pInputText`
  (text), `p-inputNumber` (number), `p-datepicker` (date), `p-select` (上架狀態), `p-checkbox`
  (允許重聽).
- **Commit on blur** (`commit()`): validate client-side first — required text non-empty, numbers
  non-negative, dates valid, and `上架日期 ≤ 下架日期`. On validation failure it shows an inline
  error and **keeps the cell in edit mode**. On success it fetches the full course
  (`getById`, to preserve the N-N lists), applies the one field, and PUTs; a **server failure
  reverts** the cell (the row object is never mutated until the save succeeds).

## FeaturedPromoItem board (上稿作業)

A **custom, single-page feature** built from `spec/custom/FeaturedPromoItem/` — it replaces the
standard list/detail/form triad with a calendar-style board. Conventions worth reusing:

- **Backend is still conventional**: `Models/FeaturedPromoItem{,Request,Query}.cs`,
  `IFeaturedPromoItemRepository` + Dapper impl, `FeaturedPromoItemsController`, registered in
  `Program.cs`. `int` IDENTITY PK (SCOPE_IDENTITY on create, no create-time 409 on the PK). `PromoCode`
  is **joined in** from `Promotion2` for display (the FK is `Promotion_pkid`).
- **Query filter** (`POST /api/featured-promo-items/query`) takes `TrainingCenterPkid` (active tab) +
  `WeekStart` (Monday); the repo matches `ScheduleOn BETWEEN WeekStart AND WeekStart+6`.
- **Unique (ScheduleOn, TrainingCenter, Slot)** constraint is enforced with a `SlotTakenAsync` 409 on
  create **and** update (update excludes its own pkid) — an addition beyond the plain IDENTITY shape.
- **Slot move**: `POST /api/featured-promo-items/{id}/move` with `{ direction: ±1 }` swaps a row with
  the occupant of the target slot inside a transaction (temp slot 0 to dodge the unique index).
- **`date` columns use C# `DateOnly`**, serialized as `"yyyy-MM-dd"` by System.Text.Json. Dapper
  2.1.66 can *read* a `date` but **cannot bind `DateOnly` as a parameter** — it throws
  `"... cannot be used as a parameter value"`. Fixed by registering `DateOnlyTypeHandler` (and a
  `TimeOnlyTypeHandler`) from `Infrastructure/DapperTypeHandlers.cs` in `Program.cs`. Verified live
  against SQL Server: the board query returns the real 3/16–3/22 week with joined PromoCodes.
- **Two new lookups** in `LookupsController`: `GET /api/lookups/training-centers` (board tabs) and
  `GET /api/lookups/promotions/{code}` (resolve a `Promotion2.PromoCode` → pkid for the edit form; 404
  if unknown).
- **Frontend** (`features/featured-promo-items/`): `featured-promo-item-board` (tabs + week nav + 3-slot
  day grid + Copy/Paste buffer + move + delete) hosts the inline `featured-promo-item-form`
  (PromoCode-lookup edit/new, emits `saved`/`cancelled`). Pure Monday–Sunday date math lives in
  `date-week.util.ts`. Single lazy route `/featured-promo-items`; nav under 首頁管理 Home.
