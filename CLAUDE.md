# CLAUDE.md

Full-stack CMS: **ASP.NET Core 9 (Dapper only, no EF)** / Angular 20 + PrimeNG / SQL Server.
All code under `src/`; backend and Angular unit tests pass here.

## Adding a feature (`/crud`)

Features are generated table-by-table from two source-of-truth inputs:

- **`database/*.sql`** — raw `CREATE TABLE` schema.
- **`spec/code-gen.convention.md`** — mandatory backend + frontend patterns. **Read before
  generating any table's code.**

Per-feature build specs follow `spec/feature-spec.template.md`; worked examples: `spec/sample1.spec.md`
(Course — FK multi-map + N-N) and `spec/sample2.spec.md` (SkillTrain — N-N). `spec/ui-sample-*.png` are
visual refs.

## Reference docs (read on demand)

- **`docs/development.md`** — commands, run order, the live `localhost:5000` DB (read-only).
- **`docs/architecture.md`** — three-layer backend, the three PK shapes, n-n, routes, frontend layout.
- **`docs/features.md`** — implemented features + their per-feature conventions.
- **`spec/auth.md`** — auth feature (login/JWT, global authorization, profile, change/reset password).

When you change something a doc describes, update that doc in the same change. When a new convention
emerges, add it to `spec/code-gen.convention.md` and flag the gap.
