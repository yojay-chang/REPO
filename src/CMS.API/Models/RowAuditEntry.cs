namespace CMS.API.Models;

/// <summary>
/// One audit-trail entry for a single record, as returned by the RowAudit query endpoint
/// (<see cref="Controllers.RowAuditController"/>). A projection of <see cref="RowAudit"/> exposing only
/// the columns the UI shows — the acting user, what happened, and when.
/// </summary>
public class RowAuditEntry
{
    /// <summary>When the change was recorded.</summary>
    public DateTime DateTime { get; set; }

    /// <summary>The user who made the change, or <c>"system"</c>.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary><c>"Insert"</c> | <c>"Update"</c> | <c>"Delete"</c>.</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>Insert/Delete: the row's first string column. Update: the changed column names.</summary>
    public string? ActionDesc { get; set; }
}
