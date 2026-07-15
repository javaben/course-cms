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
