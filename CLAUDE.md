# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## What this is

A full-stack CMS scaffolded from a SQL Server schema, following a fixed code-generation
convention. Features are added table-by-table via `/crud`, driven by two source-of-truth inputs:

- **`database/*.sql`** — the schema (raw `CREATE TABLE` scripts). `auth.sql` holds `AppRole`,
  `AppUser`, `AppUserRole`; `admin`, `course`, `promotion` are future features.
- **`spec/code-gen.convention.md`** — the mandatory backend + frontend patterns. **Read before
  generating any new table's code.** `spec/sample1.spec.md` is a worked per-feature spec (Course)
  showing the expected depth (FKs, n-n, query filters, copy actions). `spec/ui-sample-*.png` are
  visual style references only.

Everything lives under `src/`. No Entity Framework — **Dapper only**. Backend is fully built and
tested; the Angular frontend is authored to compile but **Node.js is not installed here**, so it has
never been `npm install`/`ng build`-verified.

## Reference docs (read on demand)

- **`docs/architecture.md`** — three-layer backend flow, the three PK shapes, n-n handling, routes,
  tests, and Angular/PrimeNG frontend layout. Read when generating a table or changing the layering.
- **`docs/features.md`** — the implemented features (AppRole, AppUser, PublishStatus, Partner,
  CourseGroup) and the AppUser password-handling convention.

## Commands

Backend (from `src/`, requires .NET 9 SDK):
```powershell
dotnet run --project CMS.API        # http://localhost:5000, Swagger UI at /swagger
dotnet build CMS.slnx
dotnet test                         # all xUnit tests
dotnet test --filter "FullyQualifiedName~AppRolesControllerTests.Create_returns_409"  # single test
```

Frontend (from `src/CMS.NG`, **requires Node 20+**):
```powershell
npm install
npm start                           # ng serve → http://localhost:4200
npm test                            # Karma + Jasmine
npm test -- --include src/app/features/app-roles/app-role-form/app-role-form.spec.ts   # single spec
```

## Run order for local dev

1. Create the `CMS` database on `.\SQLEXPRESS` and run `database/*.sql` (start with `auth.sql`).
2. `dotnet run --project CMS.API` (port 5000).
3. `npm start` in `CMS.NG` (port 4200).

## Keeping docs current

Update the relevant file in the same change whenever you alter something it describes — a new table
feature (`docs/features.md`), a command/port/path-alias or layering change (this file /
`docs/architecture.md`). When a new convention emerges that isn't yet in
`spec/code-gen.convention.md`, add it there and flag the gap.
