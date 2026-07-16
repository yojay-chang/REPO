using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IRowAuditRepository"/> so controller tests run without a live SQL Server. Seeded
/// with a small trail for Course #123 (plus noise for other tables/records) and applies the same
/// filter + newest-first semantics as the real repository.
/// </summary>
public class FakeRowAuditRepository : IRowAuditRepository
{
    private readonly record struct Row(string TableName, int Pkid, RowAuditEntry Entry);

    private readonly List<Row> _rows =
    [
        new("Course", 123, new RowAuditEntry { DateTime = new DateTime(2026, 6, 1, 9, 0, 0), UserName = "alice", ActionType = "Insert", ActionDesc = "Intro" }),
        new("Course", 123, new RowAuditEntry { DateTime = new DateTime(2026, 6, 4, 14, 30, 0), UserName = "bob", ActionType = "Update", ActionDesc = "Title" }),
        new("Course", 123, new RowAuditEntry { DateTime = new DateTime(2026, 6, 2, 10, 0, 0), UserName = "carol", ActionType = "Update", ActionDesc = "ListPrice" }),
        // Noise: same pkid, different table — and same table, different pkid — must be excluded.
        new("Partner", 123, new RowAuditEntry { DateTime = new DateTime(2026, 6, 5, 8, 0, 0), UserName = "dave", ActionType = "Insert", ActionDesc = "Acme" }),
        new("Course", 999, new RowAuditEntry { DateTime = new DateTime(2026, 6, 6, 8, 0, 0), UserName = "erin", ActionType = "Delete", ActionDesc = "Old" }),
    ];

    public Task<IEnumerable<RowAuditEntry>> GetForRecordAsync(string tableName, int pkid)
    {
        var entries = _rows
            .Where(r => r.TableName == tableName && r.Pkid == pkid)
            .Select(r => r.Entry)
            .OrderByDescending(e => e.DateTime)
            .AsEnumerable();
        return Task.FromResult(entries);
    }
}
