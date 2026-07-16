# CLAUDE.md

Full-stack CMS: **ASP.NET Core 9 (Dapper, no EF)** / Angular 20 + PrimeNG / SQL Server. All code in
`src/`; backend + Angular unit tests pass here. Features are built table-by-table via `/crud`.

## Reference docs — read on demand
| Doc | Covers |
|-----|--------|
| `spec/code-gen.convention.md` | **mandatory** codegen patterns — read before generating any table (also lists schema sources + spec templates) |
| `docs/architecture.md` | 3-layer backend, the 3 PK shapes, n-n, routes, frontend layout |
| `docs/features.md` | implemented features + per-feature conventions |
| `docs/development.md` | commands, run order, live `localhost:5000` DB (read-only) |
| `spec/auth.md` | auth: login/JWT, global authorization, profile, change/reset password |

Change what a doc describes → update it in the same change. New convention → `spec/code-gen.convention.md`, and flag the gap.
