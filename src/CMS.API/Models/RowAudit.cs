namespace CMS.API.Models;

/// <summary>
/// One row of the <c>RowAudit</c> table — a single Insert / Update / Delete against any business
/// table. Written by <see cref="Auditing.RowAuditWriter"/>; <see cref="Pkid"/> is IDENTITY and is
/// never supplied on INSERT.
/// </summary>
public class RowAudit
{
    public int Pkid { get; set; }

    /// <summary>The audited business table, e.g. <c>"Course"</c>.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>The signed-in user's name, or <c>"system"</c> when there is no authenticated user.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>The audited row's <c>pkid</c>, as a string.</summary>
    public string PrimaryKeyValues { get; set; } = string.Empty;

    /// <summary><c>"Insert"</c> | <c>"Update"</c> | <c>"Delete"</c>.</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Insert/Delete: the first string property's value. Update: a comma-separated list of the
    /// property names that changed. Truncated to 1000 characters.
    /// </summary>
    public string? ActionDesc { get; set; }

    /// <summary>When the change was recorded.</summary>
    public DateTime DateTime { get; set; }
}
