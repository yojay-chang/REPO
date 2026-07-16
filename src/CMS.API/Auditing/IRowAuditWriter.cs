using System.Data;

namespace CMS.API.Auditing;

/// <summary>
/// Writes one <c>RowAudit</c> row describing a change to any business table. Repositories call this
/// after a successful Insert / Update / Delete. Generic over the entity type — the primary key and
/// the description columns are discovered by reflection, so no per-entity wiring is required.
///
/// Each method has two forms: a stand-alone form that opens its own connection, and a form that takes
/// the caller's <see cref="IDbConnection"/> (and optional <see cref="IDbTransaction"/>) so the audit
/// INSERT runs on the <b>same connection / transaction</b> as the change it records — a rolled-back or
/// failed change then leaves no audit row.
/// </summary>
public interface IRowAuditWriter
{
    /// <summary>Record an insert. <c>ActionDesc</c> = the entity's first string property value.</summary>
    Task LogInsertAsync<T>(string tableName, T entity);

    /// <summary>
    /// Record an update. <c>ActionDesc</c> = a comma-separated list of the property names whose value
    /// differs between <paramref name="before"/> and <paramref name="after"/>. When nothing changed,
    /// no row is written.
    /// </summary>
    Task LogUpdateAsync<T>(string tableName, T before, T after);

    /// <summary>Record a delete. <c>ActionDesc</c> = the entity's first string property value.</summary>
    Task LogDeleteAsync<T>(string tableName, T entity);

    /// <summary>
    /// Record an insert on the caller's connection/transaction so the audit row is committed (or rolled
    /// back) atomically with the change.
    /// </summary>
    Task LogInsertAsync<T>(IDbConnection connection, IDbTransaction? transaction, string tableName, T entity);

    /// <summary>
    /// Record an update on the caller's connection/transaction. <c>ActionDesc</c> lists the changed
    /// property names; when nothing changed, no row is written.
    /// </summary>
    Task LogUpdateAsync<T>(IDbConnection connection, IDbTransaction? transaction, string tableName, T before, T after);

    /// <summary>
    /// Record a delete on the caller's connection/transaction so the audit row is committed (or rolled
    /// back) atomically with the change.
    /// </summary>
    Task LogDeleteAsync<T>(IDbConnection connection, IDbTransaction? transaction, string tableName, T entity);
}
