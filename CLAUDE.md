# CLAUDE.md

Full-stack CMS: **ASP.NET Core 9 (Dapper, no EF)** / Angular 20 + PrimeNG / SQL Server. All code in
`src/`; backend + Angular unit tests pass here. Features are built table-by-table via `/crud`.

## Cross-Cutting Conventions (mandatory every feature)

Wired through shared services — read **`docs/conventions.md`** before adding a feature; don't reinvent.

- **Row Audit** — every repository Insert/Update/Delete logs via `IRowAuditWriter` on the same
  connection/tx; every detail & form page shows the `RowAuditBadge` in the toolbar.
- **Exception handling** — global `ExceptionHandlingMiddleware` returns a safe generic 500 (no
  stack/SQL); the `authInterceptor` toasts 500s. 401/403/validation are unchanged. No per-controller
  try/catch.

## Reference docs — read on demand
| Doc | Covers |
|-----|--------|
| `spec/code-gen.convention.md` | **mandatory** codegen patterns — read before generating any table (also lists schema sources + spec templates) |
| `docs/conventions.md` | cross-cutting checklist (Row Audit, exception handling) |
| `docs/architecture.md` | 3-layer backend, the 3 PK shapes, n-n, routes, frontend layout |
| `docs/features.md` | implemented features + per-feature conventions |
| `docs/development.md` | commands, run order, live `localhost:5000` DB (read-only) |
| `spec/auth.md` | auth: login/JWT, global authorization, profile, change/reset password |

Change what a doc describes → update it in the same change. New convention → `spec/code-gen.convention.md`, and flag the gap.

## gstack

Use the **`/browse`** skill from gstack for all web browsing. **Never** use `mcp__claude-in-chrome__*`
tools.

Available gstack skills: `/office-hours`, `/plan-ceo-review`, `/plan-eng-review`,
`/plan-design-review`, `/design-consultation`, `/design-shotgun`, `/design-html`, `/review`, `/ship`,
`/land-and-deploy`, `/canary`, `/benchmark`, `/browse`, `/connect-chrome`, `/qa`, `/qa-only`,
`/design-review`, `/setup-browser-cookies`, `/setup-deploy`, `/setup-gbrain`, `/retro`, `/investigate`,
`/document-release`, `/document-generate`, `/codex`, `/cso`, `/autoplan`, `/plan-devex-review`,
`/devex-review`, `/careful`, `/freeze`, `/guard`, `/unfreeze`, `/gstack-upgrade`, `/learn`.
