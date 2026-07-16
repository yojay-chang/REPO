using System.Data;
using CMS.API.Data;
using CMS.API.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CMS.API.Tests;

/// <summary>
/// Proves the read side of the row-audit trail on the real <see cref="RowAuditRepository"/> against a
/// private in-memory SQLite database — no SQL Server required. Asserts that <c>GetForRecordAsync</c>
/// filters by <c>TableName</c> + numeric pkid and returns rows newest first.
/// </summary>
public sealed class RowAuditRepositoryTests : IDisposable
{
    private readonly string _connectionString = $"Data Source=RowAuditRepo-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly RowAuditRepository _repository;

    public RowAuditRepositoryTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        _keepAlive.Execute(@"
            CREATE TABLE RowAudit (
                pkid             INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName        TEXT NOT NULL,
                UserName         TEXT NOT NULL,
                PrimaryKeyValues TEXT NOT NULL,
                ActionType       TEXT NOT NULL,
                ActionDesc       TEXT NULL,
                [DateTime]       TEXT NOT NULL
            );");

        _repository = new RowAuditRepository(new SqliteConnectionFactory(_connectionString));
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task GetForRecord_FiltersByTableAndPkid_AndReturnsNewestFirst()
    {
        // Target: Course #123 — three changes, deliberately inserted out of chronological order.
        Seed("Course", 123, "alice", "Insert", "Intro", new DateTime(2026, 6, 1, 9, 0, 0));
        Seed("Course", 123, "bob", "Update", "Title", new DateTime(2026, 6, 4, 14, 30, 0));
        Seed("Course", 123, "carol", "Update", "ListPrice", new DateTime(2026, 6, 2, 10, 0, 0));
        // Noise: same pkid but a different table, and same table but a different pkid.
        Seed("Partner", 123, "dave", "Insert", "Acme", new DateTime(2026, 6, 5, 8, 0, 0));
        Seed("Course", 999, "erin", "Delete", "Old", new DateTime(2026, 6, 6, 8, 0, 0));

        var entries = (await _repository.GetForRecordAsync("Course", 123)).ToList();

        Assert.Equal(3, entries.Count);
        Assert.All(entries, e => Assert.Contains(e.UserName, new[] { "alice", "bob", "carol" }));

        // Newest first by DateTime.
        Assert.Equal(new DateTime(2026, 6, 4, 14, 30, 0), entries[0].DateTime);
        Assert.Equal("bob", entries[0].UserName);
        Assert.Equal("Update", entries[0].ActionType);
        Assert.Equal("Title", entries[0].ActionDesc);

        Assert.Equal(new DateTime(2026, 6, 2, 10, 0, 0), entries[1].DateTime);
        Assert.Equal(new DateTime(2026, 6, 1, 9, 0, 0), entries[2].DateTime);
    }

    [Fact]
    public async Task GetForRecord_NoMatches_ReturnsEmpty()
    {
        Seed("Course", 1, "alice", "Insert", "x", new DateTime(2026, 6, 1, 9, 0, 0));

        var entries = await _repository.GetForRecordAsync("Course", 2);

        Assert.Empty(entries);
    }

    private void Seed(string table, int pkid, string user, string action, string? desc, DateTime when) =>
        _keepAlive.Execute(
            @"INSERT INTO RowAudit (TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc, [DateTime])
              VALUES (@TableName, @UserName, @PrimaryKeyValues, @ActionType, @ActionDesc, @DateTime)",
            new
            {
                TableName = table,
                UserName = user,
                PrimaryKeyValues = pkid.ToString(),
                ActionType = action,
                ActionDesc = desc,
                DateTime = when,
            });

    private sealed class SqliteConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;
        public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
    }
}
