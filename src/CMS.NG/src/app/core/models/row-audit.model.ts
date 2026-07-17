/** One audit-trail entry for a record, as returned by `GET /api/rowaudit`. */
export interface RowAuditEntry {
  /** ISO timestamp of when the change was recorded. */
  dateTime: string;
  /** The user who made the change, or `"system"`. */
  userName: string;
  /** `"Insert"` | `"Update"` | `"Delete"`. */
  actionType: string;
  /** Insert/Delete: the row's first string column. Update: the changed column names. */
  actionDesc: string | null;
}
