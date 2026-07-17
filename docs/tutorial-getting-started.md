# Tutorial: from a fresh clone to a logged-in CMS

By the end of this you'll have the API on `http://localhost:5000`, the Angular app on
`http://localhost:4200`, and you'll be signed in as an admin looking at real course data.

The part that trips everyone up is in the middle. The `database/*.sql` files create tables but
insert **no rows** — they're schema-only. A fresh clone therefore has no config and no users, and
the app has no sign-up page, so there's nothing to log in as until you seed one by hand. Step 3 is
the step that doesn't exist anywhere else, and skipping it is why login returns a blank 500.

## What you'll need

- **.NET 9 SDK** — `dotnet --version` should print `9.x`
- **Node 20+** — `node --version` (Node 24 is fine)
- **SQL Server Express** on `.\SQLEXPRESS`, with your Windows account able to connect (the app uses
  integrated auth, so there's no DB password anywhere)
- **sqlcmd** — ships with SQL Server; `sqlcmd -?` should print usage

Run everything from a PowerShell prompt at the repo root.

## Step 1: Create the database

```powershell
sqlcmd -S .\SQLEXPRESS -E -Q "CREATE DATABASE CMS;"
```

## Step 2: Create the tables

Order matters — `auth.sql` first, because the others reference what it creates.

```powershell
sqlcmd -S .\SQLEXPRESS -d CMS -E -i database/auth.sql
sqlcmd -S .\SQLEXPRESS -d CMS -E -i database/admin.sql
sqlcmd -S .\SQLEXPRESS -d CMS -E -i database/course.sql
sqlcmd -S .\SQLEXPRESS -d CMS -E -i database/promotion.sql
```

Check it worked:

```powershell
sqlcmd -S .\SQLEXPRESS -d CMS -E -Q "SELECT COUNT(*) AS Tables FROM sys.tables;"
```

You should see a double-digit table count. Every one of those tables is empty, which is the
problem Step 3 solves.

## Step 3: Seed config and your first user

Two things are missing from a fresh database, and the app cannot start without either.

**The `appConfig` row.** The API reads its JWT signing key and its default password from
`SysConfig['appConfig']` at runtime, not from `appsettings.json` — that's why no secret ships in
this repo, and why you have to supply your own.

**A user.** Every endpoint requires an authenticated caller (`Program.cs:96` sets a fallback policy
of `RequireAuthenticatedUser`), and only `POST /api/Auth/login` opts out. So you cannot create the
first user through the API — you'd need to be logged in to do it. It has to go in via SQL.

First generate a signing key. HMAC-SHA256 needs at least 256 bits, so anything shorter than 32
characters will throw when the app signs its first token:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Copy that output. Now save this as `bootstrap.sql`, replacing the two placeholders:

```sql
USE [CMS];
GO

-- The API reads both of these at runtime, on every request that needs them.
INSERT INTO SysConfig (configKey, configValue)
VALUES ('appConfig', N'{"symmetricSecurityKey":"PASTE_YOUR_GENERATED_KEY_HERE","defaultPassword":"PICK_A_DEFAULT_PASSWORD"}');

-- RoleId must be exactly 'Admin'. [Authorize(Roles="Admin")] matches this string, not RoleName.
INSERT INTO AppRole (RoleId, RoleName, PermissionLevel, Description)
VALUES ('Admin', N'系統管理員', 1, N'Full access');

-- Your login. PasswordHash below is the SHA-256 hex of 'Passw0rd!' — change the password if you like,
-- but keep it ASCII (see the note under this block).
INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
VALUES ('admin', N'Administrator', 1,
        LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varchar(200), 'Passw0rd!')), 2)),
        GETUTCDATE());

INSERT INTO AppUserRole (UserId, RoleId) VALUES ('admin', 'Admin');
GO
```

Run it:

```powershell
sqlcmd -S .\SQLEXPRESS -d CMS -E -i bootstrap.sql
```

Then delete `bootstrap.sql`, or keep it somewhere outside the repo. It holds your signing key.

**Why a SHA-256 hash when the app uses PBKDF2.** The app stores passwords as
`pbkdf2-sha256$<iterations>$<salt>$<key>`, which you can't write by hand — the salt is random per
user. But `PasswordHasher.Verify` still accepts bare 64-character hex SHA-256 rows from before that
format existed, and reports `SuccessRehashNeeded`, so `AuthController.Login` re-hashes them to
PBKDF2 in place on the next successful login. Seeding a legacy hash and letting your first login
upgrade it is the supported migration path, not a workaround —
`AuthControllerTests.cs:277` (`Login_accepts_a_legacy_sha256_row_and_upgrades_it_in_place`) is the
test that pins the behaviour.

**Keep the bootstrap password ASCII.** `HASHBYTES` hashes the bytes of a `varchar` in the database's
codepage; the app hashes UTF-8 bytes. Those agree for ASCII and diverge for anything else, and a
divergence here just looks like a wrong password.

## Step 4: Run the API

```powershell
dotnet run --project src/CMS.API
```

A browser opens on `http://localhost:5000/swagger` with the full API surface. Leave this running and
open a second terminal.

Everything except `POST /api/Auth/login` returns 401 until you have a token — that's the fallback
policy doing its job, not a misconfiguration.

## Step 5: Run the Angular app and sign in

```powershell
cd src/CMS.NG
npm install
npm start
```

Open `http://localhost:4200` and sign in with `admin` / `Passw0rd!` (or whatever you set in Step 3).
`environment.development.ts` already points the frontend at `http://localhost:5000`, so there's
nothing to configure.

You should land on the CMS shell with the sidebar. That login just silently upgraded your seeded
SHA-256 row to PBKDF2 — confirm it:

```powershell
sqlcmd -S .\SQLEXPRESS -d CMS -E -Q "SELECT LEFT(PasswordHash, 13) AS Format FROM AppUser WHERE UserId = 'admin';"
```

`pbkdf2-sha256` means the upgrade fired. If it still shows hex, your login didn't succeed.

## What you built

A running CMS with an admin account, backed by a real SQL Server database. The tables are empty
apart from what you seeded, so 課程 (Course) and 上稿作業 (FeaturedPromoItem) will render as empty
lists until you add rows — through the UI, or by loading your own data.

Where to go next:

- [`docs/development.md`](development.md) — day-to-day commands, test invocations, run order
- [`docs/architecture.md`](architecture.md) — the 3-layer backend, the three PK shapes, n-n handling
- [`docs/features.md`](features.md) — what's built, and the conventions each feature follows
- [`docs/learnings.md`](learnings.md) — traps this codebase has already sprung on people

## Troubleshooting

**Login returns a generic 500 and the toast says nothing useful.** Your `appConfig` row is missing or
malformed. The API throws `SysConfig['appConfig'] 未設定`, and `ExceptionHandlingMiddleware`
deliberately collapses every unhandled exception into a bare 500 so nothing leaks — which means the
real reason only exists in the API console. Check the row:

```powershell
sqlcmd -S .\SQLEXPRESS -d CMS -E -Q "SELECT configKey FROM SysConfig WHERE configKey = 'appConfig';"
```

**Login returns 401 with 'invalid credentials'.** Login collapses every failure — unknown user,
inactive user, wrong password — into one generic 401 on purpose, so the message won't narrow it
down. Check the user exists and is active:

```powershell
sqlcmd -S .\SQLEXPRESS -d CMS -E -Q "SELECT UserId, IsActive, LEN(PasswordHash) AS HashLen FROM AppUser;"
```

`HashLen` of 64 is a legacy row waiting for its first login. Anything near 95 is already PBKDF2. If
the user is there and active, re-run the `HASHBYTES` insert — a non-ASCII password is the usual
culprit.

**The API console shows an `IDX…` key-size error when you try to log in.** Your
`symmetricSecurityKey` is under 32 characters — HMAC-SHA256 refuses to sign with it. Generate a new
one with the snippet in Step 3 and update the row. (`FakeSigningKeyProvider.cs` notes the same
256-bit floor for the test key.)

**The API starts but every request 401s, including in Swagger.** Expected. Swagger's UI doesn't
attach a bearer token. Call `POST /api/Auth/login`, copy the `accessToken`, and use **Authorize**.

**`Cannot open database "CMS"`.** Step 1 didn't run, or you're on a different SQL instance. Confirm
with `sqlcmd -S .\SQLEXPRESS -E -Q "SELECT name FROM sys.databases;"`.
