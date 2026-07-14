# CMS — Full-Stack App

Generated from the database schema in `../database` following `../spec/code-gen.convention.md`.

- **Backend** — `CMS.API` (.NET 9 Web API, Dapper, Swagger) + `CMS.API.Tests` (xUnit)
- **Frontend** — `CMS.NG` (Angular 20 standalone, PrimeNG)
- **First feature** — CRUD **AppRole** (角色) with the n-n `AppUser` relationship (使用者)

```
src/
├─ CMS.slnx                 # solution
├─ CMS.API/                 # .NET 9 Web API
│  ├─ Controllers/          # AppRolesController, LookupsController
│  ├─ Models/               # AppRole, AppRoleRequest, AppRoleQuery, AppUserLookup
│  ├─ Repositories/         # Dapper repositories (I*/impl)
│  └─ Infrastructure/       # IDbConnectionFactory / SqlConnectionFactory
├─ CMS.API.Tests/           # xUnit — AppRole endpoints (list/filter/view/add/edit)
└─ CMS.NG/                  # Angular 20 app
   └─ src/app/
      ├─ core/{models,services}
      └─ features/app-roles/{app-role-list,app-role-detail,app-role-form}
```

---

## 1. Database

Create the `CMS` database on `.\SQLEXPRESS` and run the scripts in `../database` (start with
`auth.sql`, which contains `AppRole`, `AppUser`, `AppUserRole`).

Connection string (in `CMS.API/appsettings.json`):

```
Server=.\SQLEXPRESS;Database=CMS;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False
```

---

## 2. Backend — CMS.API

Requires the **.NET 9 SDK** (verified with SDK 9.0.314 / 10.0.300).

```powershell
# from src/
dotnet run --project CMS.API      # http://localhost:5000  →  Swagger UI at /swagger
dotnet test                       # runs the 12 xUnit tests (green)
```

CORS is open to any `localhost` origin, so the Angular dev server (4200) can call the API directly.

### AppRole API

| Method | Route                        | Description                        |
|--------|------------------------------|------------------------------------|
| GET    | `/api/app-roles`             | All roles (with 使用者數 count)     |
| POST   | `/api/app-roles/query`       | Filtered search (`keyword`, `permissionLevel`) |
| GET    | `/api/app-roles/{id}`        | Single role by RoleId (+ assigned UserIds) |
| POST   | `/api/app-roles`             | Create (409 if RoleId exists)      |
| PUT    | `/api/app-roles`             | Update (RoleId in body; PK immutable) |
| DELETE | `/api/app-roles/{id}`        | Delete                             |
| GET    | `/api/lookups/app-users`     | AppUser lookup for the n-n picker  |

`RoleId` is the string primary key → route uses `{id}` (no `:int`) and the Angular service
`encodeURIComponent`s it. The n-n `AppUserRole` rows are delete-then-reinsert on save.

---

## 3. Frontend — CMS.NG

> ⚠️ **Node.js is required and was NOT installed on the generating machine**, so the Angular
> project was authored but not `npm install`/`ng build`-verified here. Install **Node 20+**, then:

```powershell
# from src/CMS.NG/
npm install
npm start                 # ng serve → http://localhost:4200
npm test                  # ng test  → Karma + Jasmine
```

- API base URL comes from `src/environments/environment*.ts` (no proxy). Dev points at
  `http://localhost:5000`; `ng serve` uses `environment.development.ts` via `angular.json`
  `fileReplacements`.
- Shorthand TS path aliases (`tsconfig.json`): `@env/*`, `@app/*`, `@core/*`, `@features/*`.
- Sidebar: **系統管理 Admin → 角色 AppRole** (`/app-roles`). List (with filter drawer + session-storage
  state), View, Add, Edit — matching the `../spec/ui-sample-*.png` layout.

---

## Run order

1. Create/populate the `CMS` database.
2. `dotnet run --project CMS.API` (port 5000).
3. `npm start` in `CMS.NG` (port 4200) → open http://localhost:4200.
