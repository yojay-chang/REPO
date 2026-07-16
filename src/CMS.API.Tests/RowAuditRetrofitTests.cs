using System.Data;
using System.Security.Claims;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Repositories;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

namespace CMS.API.Tests;

/// <summary>
/// Proves the row-audit retrofit end-to-end on a real repository (<see cref="PublishStatusRepository"/>)
/// driven against a private in-memory SQLite database — no SQL Server required. It exercises the whole
/// path (repository → <see cref="RowAuditWriter"/> → SQL INSERT), asserting that each mutation writes the
/// correct RowAudit row, and that a failed change (duplicate PK) leaves none because the audit INSERT runs
/// inside the same transaction as the change it records.
///
/// PublishStatus is chosen because its SQL is portable (user-assigned PK, no SCOPE_IDENTITY, no joins), so
/// the very repository the app runs in production executes here unmodified.
/// </summary>
public sealed class RowAuditRetrofitTests : IDisposable
{
    private const string ActingUser = "tester";

    // A uniquely-named shared in-memory database, kept alive by a single open connection for the test's
    // lifetime; every CreateConnection() opens a fresh connection onto the same shared data.
    private readonly string _connectionString = $"Data Source=RowAuditRetrofit-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly PublishStatusRepository _repository;

    public RowAuditRetrofitTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        CreateSchema(_keepAlive);

        var factory = new SqliteConnectionFactory(_connectionString);
        var audit = new RowAuditWriter(factory, StubHttpContext(ActingUser));
        _repository = new PublishStatusRepository(factory, audit);
    }

    public void Dispose() => _keepAlive.Dispose();

    // ---- Insert ------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_WritesInsertAuditRow_WithFirstStringColumn()
    {
        await _repository.CreateAsync(new PublishStatusRequest
        {
            Pkid = 10,
            Description = "草稿",
            IsDraft = true,
        });

        var audit = Assert.Single(ReadAudit());
        Assert.Equal("PublishStatus", audit.TableName);
        Assert.Equal("Insert", audit.ActionType);
        Assert.Equal("10", audit.PrimaryKeyValues);
        Assert.Equal("草稿", audit.ActionDesc);          // first string column = Description
        Assert.Equal(ActingUser, audit.UserName);
    }

    // ---- Update ------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_WritesUpdateAuditRow_ListingExactlyTheChangedColumns()
    {
        // Seed directly so only the update produces an audit row.
        SeedStatus(new PublishStatus { Pkid = 20, Description = "草稿", IsDraft = true, IsPublished = false, IsDiscontinued = false });

        var ok = await _repository.UpdateAsync(new PublishStatusRequest
        {
            Pkid = 20,
            Description = "已發布",   // changed
            IsDraft = true,           // unchanged
            IsPublished = true,       // changed
            IsDiscontinued = false,   // unchanged
        });

        Assert.True(ok);
        var audit = Assert.Single(ReadAudit());
        Assert.Equal("PublishStatus", audit.TableName);
        Assert.Equal("Update", audit.ActionType);
        Assert.Equal("20", audit.PrimaryKeyValues);
        Assert.Equal("Description, IsPublished", audit.ActionDesc);   // exactly the changed columns, in order
        Assert.Equal(ActingUser, audit.UserName);
    }

    [Fact]
    public async Task Update_WritesNoAuditRow_WhenNothingChanged()
    {
        SeedStatus(new PublishStatus { Pkid = 21, Description = "草稿", IsDraft = true, IsPublished = false, IsDiscontinued = false });

        // Same values → no column changed → BuildUpdate returns null → no audit row.
        var ok = await _repository.UpdateAsync(new PublishStatusRequest
        {
            Pkid = 21,
            Description = "草稿",
            IsDraft = true,
            IsPublished = false,
            IsDiscontinued = false,
        });

        Assert.True(ok);
        Assert.Empty(ReadAudit());
    }

    // ---- Delete ------------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_WritesDeleteAuditRow_WithFirstStringColumn()
    {
        SeedStatus(new PublishStatus { Pkid = 30, Description = "已停用", IsDraft = false, IsPublished = false, IsDiscontinued = true });

        var ok = await _repository.DeleteAsync(30);

        Assert.True(ok);
        var audit = Assert.Single(ReadAudit());
        Assert.Equal("PublishStatus", audit.TableName);
        Assert.Equal("Delete", audit.ActionType);
        Assert.Equal("30", audit.PrimaryKeyValues);
        Assert.Equal("已停用", audit.ActionDesc);   // first string column = Description
        Assert.Equal(ActingUser, audit.UserName);
    }

    // ---- Failed change leaves no audit row ---------------------------------------------------------

    [Fact]
    public async Task FailedInsert_WritesNoAuditRow_AndLeavesNoDataRow()
    {
        // Pre-existing row with the same user-assigned PK; the second insert violates the PK constraint.
        SeedStatus(new PublishStatus { Pkid = 40, Description = "草稿", IsDraft = true, IsPublished = false, IsDiscontinued = false });

        await Assert.ThrowsAnyAsync<Exception>(() => _repository.CreateAsync(new PublishStatusRequest
        {
            Pkid = 40,
            Description = "重複",
            IsDraft = true,
        }));

        // The whole transaction rolled back: no audit row, and the original data row is untouched.
        Assert.Empty(ReadAudit());
        var descriptions = _keepAlive.Query<string>("SELECT Description FROM PublishStatus WHERE pkid = 40").ToList();
        Assert.Equal(new[] { "草稿" }, descriptions);
    }

    // ---- Helpers -----------------------------------------------------------------------------------

    private static void CreateSchema(IDbConnection db)
    {
        db.Execute(@"
            CREATE TABLE PublishStatus (
                pkid           INTEGER PRIMARY KEY,
                Description    TEXT    NOT NULL,
                IsDraft        INTEGER NOT NULL,
                IsPublished    INTEGER NOT NULL,
                IsDiscontinued INTEGER NOT NULL
            );

            CREATE TABLE RowAudit (
                pkid             INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName        TEXT NOT NULL,
                UserName         TEXT NOT NULL,
                PrimaryKeyValues TEXT NOT NULL,
                ActionType       TEXT NOT NULL,
                ActionDesc       TEXT NULL,
                [DateTime]       TEXT NOT NULL
            );");
    }

    private void SeedStatus(PublishStatus s) => _keepAlive.Execute(
        @"INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
          VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued)", s);

    private List<AuditRow> ReadAudit() => _keepAlive.Query<AuditRow>(
        "SELECT TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc FROM RowAudit ORDER BY pkid").ToList();

    private sealed record AuditRow(string TableName, string UserName, string PrimaryKeyValues, string ActionType, string? ActionDesc);

    private sealed class SqliteConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;
        public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
    }

    /// <summary>An <see cref="IHttpContextAccessor"/> whose request carries a single <c>userName</c> claim.</summary>
    private static IHttpContextAccessor StubHttpContext(string userName)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("userName", userName) }, "jwt");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }
}
