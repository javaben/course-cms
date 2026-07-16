/** One RowAudit history entry for a record, as returned by GET /api/rowaudit (camelCase of RowAuditEntry). */
export interface RowAuditEntry {
  dateTime: string; // ISO datetime string
  userName: string;
  actionType: string; // "Insert" | "Update" | "Delete"
  actionDesc?: string | null;
}

/** One row of the global audit log (all tables) — GET /api/rowaudit/all, POST /api/rowaudit/query. */
export interface RowAuditListItem {
  pkid: number; // the audit row's own id (table dataKey)
  tableName: string;
  primaryKeyValues: string;
  userName: string;
  actionType: string;
  actionDesc?: string | null;
  dateTime: string;
}

/** Filter for the global audit log (all optional). */
export interface RowAuditQuery {
  tableName?: string | null;
  actionType?: string | null;
  keyword?: string | null;
}
