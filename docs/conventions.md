# Cross-Cutting Conventions

Mandatory for **every** feature. Wired through shared services — do not reinvent per feature. The
CLAUDE.md summary points here; this file is the full checklist.

## Row Audit (every table write is logged)

Backend — inject `IRowAuditWriter` into each repository and call it inside the operation's
connection/transaction so a rolled-back or failed change leaves **no** audit row:

- [ ] **Insert** — after the row exists and its pkid is known: `LogInsertAsync(conn, tx, "<Table>", newRow)`.
- [ ] **Update** — load the existing row **first** (the `before`), apply the update, reload the `after`,
      then `LogUpdateAsync(conn, tx, "<Table>", before, after)`. `before`/`after` are the **same loaded
      model type** (compared scalar-column-by-column; nav objects & n-n lists are ignored).
- [ ] **Delete** — load the row **first** so its first string column is captured, delete, then
      `LogDeleteAsync(conn, tx, "<Table>", row)`.
- [ ] Only log when the change actually succeeded (skip when the row wasn't found / nothing changed).

Audit-row rules (handled by `RowAuditWriter` via reflection — don't hand-roll):
`ActionDesc` = Insert/Delete → the row's **first string-type column value**; Update → **comma-separated
changed column names** (empty ⇒ no row written). `PrimaryKeyValues` = the `pkid` property as a string.
`UserName` = JWT user (`userName` claim), fallback `"system"`. `TableName` = the real DB table name.
`DateTime` = now. **Never insert `pkid`** (IDENTITY).

Frontend — every **detail page and form page** places the reusable `RowAuditBadge`
(`@app/shared/row-audit-badge`) at the **start of the header toolbar** (`.actions` `#start` slot):
`<app-row-audit-badge [tableName]="'<Table>'" [pkid]="<record pkid or null>">`. It shows the latest
change inline and opens the full trail on click via `GET /api/rowaudit?tableName=&pkid=` (newest first).
Pass `null`/`0` on a new record → it shows a neutral "no history" state and makes no request.

Global log — read-only page `/row-audits` (系統管理 Admin → 異動紀錄 RowAudit) lists every table's audit
rows, newest first (capped 500), filterable by table/action/keyword. Endpoints: `GET /api/rowaudit/all`,
`POST /api/rowaudit/query` (`{ tableName?, actionType?, keyword? }`), `GET /api/rowaudit/tables`.

## Exception handling (fail safe, never leak)

- [ ] Rely on the global `ExceptionHandlingMiddleware` (registered outermost in `Program.cs`) — it logs
      full detail server-side and returns ONE generic `500 { message: "An unexpected error occurred." }`.
      **Do not** add per-controller `try/catch` for unexpected errors, and never return stack traces,
      SQL, or connection details.
- [ ] Keep meaningful responses as-is: **401** (unauthenticated), **403** (forbidden), and
      **validation/400** are produced without throwing and must pass through unchanged.
- [ ] Frontend: the `authInterceptor` surfaces **500-class** errors as a friendly toast (using the safe
      body message); **401** still clears the session and redirects to Login; other statuses (e.g. 400)
      pass through so forms surface them.
