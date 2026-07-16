using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class RowAuditRepository : IRowAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RowAuditRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // PrimaryKeyValues is stored as text (the writer serializes the numeric pkid with .ToString()),
    // so the record's pkid is matched as a string. Newest first, with the audit pkid as a stable
    // tie-breaker for changes recorded within the same clock tick.
    private const string SelectSql = @"
        SELECT [DateTime], UserName, ActionType, ActionDesc
        FROM RowAudit
        WHERE TableName = @TableName AND PrimaryKeyValues = @PrimaryKeyValues
        ORDER BY [DateTime] DESC, pkid DESC";

    public async Task<IEnumerable<RowAuditEntry>> GetForRecordAsync(string tableName, int pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<RowAuditEntry>(
            SelectSql,
            new { TableName = tableName, PrimaryKeyValues = pkid.ToString() });
    }
}
