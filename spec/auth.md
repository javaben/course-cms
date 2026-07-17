# Auth Spec (登入／授權／個人資料)

Authentication, JWT authorization, and self-service account management for the CMS. This is a
**non-CRUD** feature: no table of its own — it operates over the existing `AppUser`, `AppUserRole`,
and `SysConfig` tables. All backend code lives under `CMS.API`; frontend under `CMS.NG`.

Reference tables (`database/auth.sql`):
- `AppUser(UserId PK, UserName, IsActive bit, PasswordHash nvarchar(800), PasswordUpdatedTime datetime null)`
- `AppUserRole(UserId, RoleId)` — n-n user↔role
- `SysConfig(configKey PK, configValue nvarchar(4000))` — row `configKey='appConfig'` holds a JSON object
  with (at least) `symmetricSecurityKey` and `defaultPassword`.

**Password rule everywhere:** `PasswordHash = PasswordHasher.Hash(password)` — PBKDF2-HMAC-SHA256,
per-user random salt, 210 000 iterations, stored as the self-describing string
`pbkdf2-sha256$<iterations>$<base64 salt>$<base64 key>` (~95 chars, fits `nvarchar(800)`). Because the
salt is random, **the same password hashes to a different string every time**: never compare hashes,
always call `PasswordHasher.Verify`. A hash is **never** sent to or from the client.

**Legacy rows:** before this, hashes were unsalted single-round SHA-256 as lowercase hex (64 chars).
`Verify` still accepts that format and returns `SuccessRehashNeeded`; `Login` then re-hashes and
rewrites the row in place, so old rows migrate on their owner's next successful login with no
migration script and no forced reset. Nothing writes the legacy format any more.

---

## Backend

### Shared infrastructure

- **`Infrastructure/PasswordHasher`** — the one hashing component. `Hash(password) → string` for every
  write (change, reset, AppUser create/reset); `Verify(storedHash, password) → PasswordVerificationResult`
  (`Failed` / `Success` / `SuccessRehashNeeded`) for every check. Comparison is constant-time; a
  malformed stored hash returns `Failed` rather than throwing, so a corrupt row can't 500 the login
  path. `Sha256Hex` remains `internal`, used **only** to verify and migrate legacy rows.
- **`Infrastructure/AppConfig.GetRequiredProperty(json, name) → string`** — extracts a required string
  from the `SysConfig['appConfig']` JSON blob; throws `InvalidOperationException` (→ 500) if the blob
  is missing/invalid or the property absent/blank. Read at runtime — never hard-coded.
- **`Infrastructure/SigningKeyProvider` (`ISigningKeyProvider`, singleton)** — reads
  `appConfig.symmetricSecurityKey` (via `AppConfig`), caches it, and exposes `GetSigningKey()` /
  `GetSecurityKey()`. The **same provider both issues and validates** tokens.
- **`Infrastructure/PasswordPolicy`** — `IsComplexEnough(password)` = length ≥ 8 **AND** ≥ 3 of the 4
  classes (uppercase, lowercase, digit, symbol=non-alphanumeric). `ComplexityMessage` is the exact
  rejection text (below).
- **`AuthController`** uses `[Route("api/[controller]")]` → `api/Auth`. **No class-level
  `[AllowAnonymous]`** — only `Login` opts out; every other action is protected.

### Global authorization (`Program.cs`)

- `AddAuthentication().AddJwtBearer()`; options bound to `ISigningKeyProvider` via
  `AddOptions<JwtBearerOptions>().Configure<ISigningKeyProvider>(...)` so the key is resolved from DI at
  runtime. `TokenValidationParameters`: validate signing key + lifetime; **don't** validate
  issuer/audience; `ClockSkew = 1 min`; `RoleClaimType = "role"`, `NameClaimType = "userName"`;
  `MapInboundClaims = false`.
- `app.UseAuthentication()` **before** `app.UseAuthorization()`.
- **Fallback policy** `RequireAuthenticatedUser()` — every endpoint requires a valid bearer token
  unless it carries `[AllowAnonymous]`. Any request without a valid token → **401**.

### Endpoints

| Method & route | Auth | Body | Success | Notes |
|---|---|---|---|---|
| `POST /api/Auth/login` | `[AllowAnonymous]` | `{ userId, password }` | 200 `LoginResponse` | issues JWT |
| `PUT /api/Auth/profile` | `[Authorize]` | `{ userName }` | 200 `ProfileResponse` | renames self |
| `POST /api/Auth/change-password` | `[Authorize]` | `{ currentPassword, newPassword, confirmNewPassword }` | 204 | changes own password |
| `POST /api/Auth/reset-password` | `[Authorize(Roles="Admin")]` | `{ userId }` | 204 | Admin resets a user to default |

**1. Login.** Load the user by `userId`. Return a single generic `401 { message: "invalid credentials" }`
if any of: user missing, `IsActive != 1`, or `PasswordHasher.Verify(PasswordHash, password) == Failed` —
evaluated together so the caller can't tell which failed. If `Verify` returns `SuccessRehashNeeded`,
re-hash the (in-hand) plaintext and call `UpgradePasswordHashAsync` before issuing the token: login is
the only flow that ever holds the plaintext, so it is the only place a legacy row can migrate. That
upgrade is best-effort and must never fail an otherwise-valid login. On success build a JWT:
- HMAC-SHA256, signed with the SysConfig key (via `ISigningKeyProvider`).
- Claims: `sub` = UserId, `userId` = UserId, `userName` = UserName, and one **`role`** claim per RoleId
  from `AppUserRole`.
- 24h lifetime (`AuthController.TokenLifetime`). Built via the `JwtSecurityToken` constructor (claim
  types verbatim).
- Returns `LoginResponse { userId, userName, accessToken }`. `PasswordHash` lives only on the
  server-side `AuthUser` model and is never serialized.

**2. Update profile (self).** UserId comes from the JWT (`User.FindFirstValue("userId")`), **never** the
body. `UpdateProfileRequest` carries only `UserName` (required, trimmed; blank → 400). UserId and roles
are unchangeable here. Returns `ProfileResponse { userId, userName }` (canonical, trimmed).

**3. Change password (self).** UserId from the JWT. In order:
1. `PasswordHasher.Verify(storedHash, currentPassword)` must not be `Failed` — else
   `400 { message: "目前密碼不正確。" }`, change nothing.
2. `PasswordPolicy.IsComplexEnough(newPassword)` — else `400 { message: <complexity message> }`.
3. `newPassword == confirmNewPassword` — else `400 { message: "新密碼與確認密碼不一致。" }`.
4. Store `PasswordHash = PasswordHasher.Hash(newPassword)`, `PasswordUpdatedTime = now`. Return **204**.
   (No rehash step needed here: `Hash` always writes the current format, so changing a password
   inherently upgrades a legacy row.)

   Complexity message (returned verbatim; shown bilingually in the UI):
   > 密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號
   >
   > (Password must be at least 8 characters and contain at least 3 of the 4 classes:
   > uppercase / lowercase / digit / symbol.)

**4. Reset password to default (Admin only).** `[Authorize(Roles="Admin")]` → a non-Admin authenticated
caller gets **403**, unauthenticated gets 401. For the target `userId`: read `appConfig.defaultPassword`
at runtime, set `PasswordHash = PasswordHasher.Hash(default)` and `PasswordUpdatedTime = now`. Returns
**204** (404 if the user doesn't exist). No password/hash in the response. Each reset salts afresh, so
users sharing the default password do **not** share a stored hash.

### Repository (`IAuthRepository` / `AuthRepository`, Dapper)

- `FindByUserIdAsync` — returns `AuthUser` (incl. `PasswordHash` + role ids) or null.
- `UpdateUserNameAsync(userId, userName)`.
- `ChangePasswordAsync(userId, newHash)` — sets hash + `PasswordUpdatedTime = now`.
- `ResetPasswordToDefaultAsync(userId)` — reads `appConfig.defaultPassword`, hashes, sets hash +
  `PasswordUpdatedTime = now`.
- `UpgradePasswordHashAsync(userId, newHash)` — rewrites the hash **only**, after a rehash-on-login.
  `PasswordUpdatedTime` is deliberately **not** bumped: the password didn't change, only its encoding,
  and that column means "when the password last changed".

### Backend tests

- **Unit** (`AuthControllerTests`, `Fakes/InMemoryAuthRepository` + `FakeSigningKeyProvider`): login happy
  path + generic 401s + role claims + ~24h expiry + no-hash-leak; profile updates JWT user only, ignores
  body userId, rejects blank name, 401 without claim; change-password valid path (new hash verifies +
  timestamp bumped), wrong-current changes nothing, weak-password rejected with the policy message,
  mismatch rejected. `PasswordPolicyTests` and `AppConfigTests` cover those helpers directly.
- **Integration** (`AuthorizationTests`, `WebApplicationFactory<Program>` + `ConfigureTestServices`
  swapping the key provider + repos; a fresh factory per test for isolation): protected endpoint 401
  without/with garbage token, 200 with a real token; login anonymous; `/profile` requires auth + ignores
  body userId + 400 on blank; **reset-password**: 401 without token, **403 for non-Admin**, and an Admin
  reset makes `PasswordHash` verify against the default + bumps the timestamp with no hash in the
  response. `AuthControllerTests` also cover the hash format itself: salted (two users sharing a
  password get different rows), never the plaintext or a bare digest, and rehash-on-login for a planted
  legacy SHA-256 row (accepted + upgraded in place, `PasswordUpdatedTime` untouched; a wrong password
  against a legacy row neither authenticates nor upgrades).

---

## Frontend (`CMS.NG`)

- **`core/services/auth.service.ts`** — `login` / `logout` / `updateUserName` / `changePassword` /
  `resetPasswordToDefault`. Stores the profile in **session storage** (key `auth-profile`, **not** local
  storage). Signals: `profile`, `roles` (decoded from the JWT `role` claim, base64url), `isAdmin`. No
  separate roles call.
- **`core/interceptors/auth.interceptor.ts`** (registered via `withInterceptors`) — attaches
  `Authorization: Bearer <token>` to every request; on a 401 clears the session and redirects to
  `/login` (the login request is exempt so its error surfaces on the page).
- **`core/guards/auth.guard.ts`** (`canActivateChild`) wraps all app routes; `/login` is the only public
  route.
- **`features/auth/login`** — public login page.
- **`features/auth/profile`** — "My Profile": UserId + roles **read-only**, UserName **editable**, and a
  **Change Password** form (current / new / confirm). Client validation via
  `features/auth/password-policy.ts` (`isPasswordComplex`, `passwordComplexityValidator`,
  `passwordsMatchValidator`; bilingual complexity message). On UserName save the shell + session refresh.
- **`features/app-users/app-user-form`** — **Reset Password to Default** button, **visible only to
  Admins** (`auth.isAdmin`), shown in edit mode; confirms first; calls
  `AuthService.resetPasswordToDefault(userId)` (sends only the UserId). The backend still enforces Admin.
- **App shell** (`app.ts`/`app.html`): chrome renders only when authenticated; header shows UserName +
  **My Profile** + **Logout**; the **系統管理 Admin** nav group is hidden unless roles include `Admin`.
- **Frontend tests**: `auth.service.spec`, `auth.interceptor.spec`, `auth.guard.spec`, `app.spec`
  (Admin-menu visibility), `login.spec`, `profile.spec` (read-only fields + save + change-password client
  validation), `password-policy.spec`, `app-user-form.spec` (reset button Admin-only).

## Security invariants

1. No password **hash** is ever sent to or from the client — only plaintext passwords inbound to the
   endpoints, and success/failure (+ generic message) outbound.
2. Identity for self-service actions (profile, change-password) always comes from the **JWT**, never the
   request body.
3. Privileged actions (reset-password) are enforced **server-side by role**, not merely hidden in the UI.
4. Secrets (`symmetricSecurityKey`, `defaultPassword`) are read from `SysConfig` at runtime — never
   hard-coded.
