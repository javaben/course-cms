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
| FeaturedPromoItem | 上稿作業 | 首頁管理 Home | `int` IDENTITY PK | **custom board** (not the triad); FKs to TrainingCenter + Promotion2; see below |

## AppUser password handling

A convention **not yet in `spec/code-gen.convention.md`** — flag when generalizing.

- `PasswordHash` (`nvarchar(800)`) is **backend-only** — excluded from `AppUserRequest`, every
  SELECT, and all Angular models (never sent to or from the client).
- On **create** the repository reads `SysConfig.configValue WHERE configKey='appConfig'` (a JSON
  object), extracts `defaultPassword`, SHA-256 hashes it to **lowercase hex**, and stores it (with
  `PasswordUpdatedTime = UtcNow`).
- **Update** never touches the hash.
- `POST /api/app-users/{id}/reset-password` re-hashes the default and bumps `PasswordUpdatedTime` —
  the only update path that changes the hash (`重設密碼` button in the detail/edit views).
- `PasswordUpdatedTime` is `datetime` NULL, read-only, displayed with the `+ 'Z'` UTC fix.
- The `app-roles` lookup (`GET /api/lookups/app-roles`) feeds the AppUser role picker.

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
