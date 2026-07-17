using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Reads the row-audit trail written by <see cref="Auditing.RowAuditWriter"/>.</summary>
public interface IRowAuditRepository
{
    /// <summary>
    /// The audit entries for one record — matched by <paramref name="tableName"/> and the record's
    /// numeric <paramref name="pkid"/> — ordered newest first.
    /// </summary>
    Task<IEnumerable<RowAuditEntry>> GetForRecordAsync(string tableName, int pkid);
}
