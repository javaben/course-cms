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

## Cross-Cutting Conventions (mandatory every feature)

Wired through shared services — read **`docs/conventions.md`** before adding a feature; don't reinvent.

- **Row Audit** — every repository Insert/Update/Delete logs via `IRowAuditWriter` on the same
  connection/tx; every detail & form page shows the `RowAuditBadge` in the toolbar.
- **Exception handling** — global `ExceptionHandlingMiddleware` returns a safe generic 500 (no
  stack/SQL); the `authInterceptor` toasts 500s. 401/403/validation are unchanged. No per-controller
  try/catch.

## Reference docs (read on demand)

- **`docs/development.md`** — commands, run order, the live `localhost:5000` DB (read-only).
- **`docs/architecture.md`** — three-layer backend, the three PK shapes, n-n, routes, frontend layout.
- **`docs/features.md`** — implemented features + their per-feature conventions.
- **`docs/conventions.md`** — cross-cutting checklist (Row Audit, exception handling).
- **`spec/auth.md`** — auth feature (login/JWT, global authorization, profile, change/reset password).

When you change something a doc describes, update that doc in the same change. When a new convention
emerges, add it to `spec/code-gen.convention.md` and flag the gap.

## gstack

Use the **`/browse`** skill from gstack for all web browsing. **Never** use `mcp__claude-in-chrome__*`
tools.

Available gstack skills: `/office-hours`, `/plan-ceo-review`, `/plan-eng-review`,
`/plan-design-review`, `/design-consultation`, `/design-shotgun`, `/design-html`, `/review`, `/ship`,
`/land-and-deploy`, `/canary`, `/benchmark`, `/browse`, `/connect-chrome`, `/qa`, `/qa-only`,
`/design-review`, `/setup-browser-cookies`, `/setup-deploy`, `/setup-gbrain`, `/retro`, `/investigate`,
`/document-release`, `/document-generate`, `/codex`, `/cso`, `/autoplan`, `/plan-devex-review`,
`/devex-review`, `/careful`, `/freeze`, `/guard`, `/unfreeze`, `/gstack-upgrade`, `/learn`.
