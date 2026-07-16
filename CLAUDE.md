# CLAUDE.md

Full-stack CMS: **ASP.NET Core 9 (Dapper only, no EF)** / Angular 20 + PrimeNG / SQL Server.
All code under `src/`; backend and Angular unit tests pass here.

Features are generated table-by-table via `/crud`. **Before generating any table, read
`spec/code-gen.convention.md`** — mandatory backend + frontend patterns, plus schema sources and spec templates.

## Reference docs — read on demand

| Doc | Covers |
|-----|--------|
| `docs/development.md` | commands, run order, live `localhost:5000` DB (read-only) |
| `docs/architecture.md` | 3-layer backend, the 3 PK shapes, n-n, routes, frontend layout |
| `docs/features.md` | implemented features + their per-feature conventions |
| `spec/code-gen.convention.md` | **mandatory** codegen patterns — read before generating any table |
| `spec/auth.md` | auth feature (login/JWT, global authorization, profile, change/reset password) |

Change something a doc describes → update that doc in the same change. New convention →
add it to `spec/code-gen.convention.md` and flag the gap.
