# Development

## Commands

Backend (from `src/`, requires .NET 9 SDK):
```powershell
dotnet run --project CMS.API        # http://localhost:5000, Swagger UI at /swagger
dotnet build CMS.slnx
dotnet test                         # all xUnit tests
dotnet test --filter "FullyQualifiedName~AppRolesControllerTests.Create_returns_409"  # single test
```

Frontend (from `src/CMS.NG`, requires Node 20+; Node 24 + `node_modules` present here):
```powershell
npm install
npm start                           # ng serve → http://localhost:4200
npm test                            # Karma + Jasmine
npm test -- --include src/app/features/app-roles/app-role-form/app-role-form.spec.ts   # single spec
```

`npx ng build` and `npx ng test --no-watch --browsers=ChromeHeadless` both pass in this repo.

## Run order for local dev

1. Create the `CMS` database on `.\SQLEXPRESS` and run `database/*.sql` (start with `auth.sql`).
2. `dotnet run --project CMS.API` (port 5000).
3. `npm start` in `CMS.NG` (port 4200).

## Live database

A SQL Server (`CMS` DB) is reachable at `localhost:5000` when the API is running, so read endpoints
can be smoke-tested with real data. It holds production-like rows — **keep live testing read-only;
do not mutate it.**
