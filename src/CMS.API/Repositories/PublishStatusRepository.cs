using System.Data;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class PublishStatusRepository : IPublishStatusRepository
{
    private const string TableName = "PublishStatus";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRowAuditWriter _audit;

    public PublishStatusRepository(IDbConnectionFactory connectionFactory, IRowAuditWriter audit)
    {
        _connectionFactory = connectionFactory;
        _audit = audit;
    }

    private const string SelectColumns = @"
        SELECT s.pkid, s.Description, s.IsDraft, s.IsPublished, s.IsDiscontinued
        FROM PublishStatus s";

    public async Task<IEnumerable<PublishStatus>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<PublishStatus>($"{SelectColumns} ORDER BY s.pkid ASC");
    }

    public async Task<IEnumerable<PublishStatus>> QueryAsync(PublishStatusQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("s.Description LIKE @Keyword");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.IsDraft.HasValue)
        {
            where.Add("s.IsDraft = @IsDraft");
            parameters.Add("IsDraft", query.IsDraft.Value);
        }

        if (query.IsPublished.HasValue)
        {
            where.Add("s.IsPublished = @IsPublished");
            parameters.Add("IsPublished", query.IsPublished.Value);
        }

        if (query.IsDiscontinued.HasValue)
        {
            where.Add("s.IsDiscontinued = @IsDiscontinued");
            parameters.Add("IsDiscontinued", query.IsDiscontinued.Value);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY s.pkid ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<PublishStatus>(sql, parameters);
    }

    public async Task<PublishStatus?> GetByIdAsync(byte pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<PublishStatus>(
            $"{SelectColumns} WHERE s.pkid = @Pkid", new { Pkid = pkid });
    }

    public async Task<bool> ExistsAsync(byte pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PublishStatus WHERE pkid = @Pkid", new { Pkid = pkid });
        return count > 0;
    }

    public async Task<byte> CreateAsync(PublishStatusRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        await db.ExecuteAsync(
            @"INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
              VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued)",
            new
            {
                request.Pkid,
                request.Description,
                request.IsDraft,
                request.IsPublished,
                request.IsDiscontinued
            }, tx);

        var created = await ReadForAuditAsync(db, tx, request.Pkid);
        await _audit.LogInsertAsync(db, tx, TableName, created);

        tx.Commit();
        return request.Pkid;
    }

    public async Task<bool> UpdateAsync(PublishStatusRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await ReadForAuditAsync(db, tx, request.Pkid);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        await db.ExecuteAsync(
            @"UPDATE PublishStatus
                 SET Description = @Description,
                     IsDraft = @IsDraft,
                     IsPublished = @IsPublished,
                     IsDiscontinued = @IsDiscontinued
               WHERE pkid = @Pkid",
            new
            {
                request.Pkid,
                request.Description,
                request.IsDraft,
                request.IsPublished,
                request.IsDiscontinued
            }, tx);

        var after = await ReadForAuditAsync(db, tx, request.Pkid);
        await _audit.LogUpdateAsync(db, tx, TableName, before, after);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(byte pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await ReadForAuditAsync(db, tx, pkid);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        await db.ExecuteAsync("DELETE FROM PublishStatus WHERE pkid = @Pkid", new { Pkid = pkid }, tx);
        await _audit.LogDeleteAsync(db, tx, TableName, before);

        tx.Commit();
        return true;
    }

    /// <summary>Read the current row (base columns only) on the caller's connection/transaction for auditing.</summary>
    private static Task<PublishStatus?> ReadForAuditAsync(IDbConnection db, IDbTransaction tx, byte pkid) =>
        db.QuerySingleOrDefaultAsync<PublishStatus>(
            $"{SelectColumns} WHERE s.pkid = @Pkid", new { Pkid = pkid }, tx);
}
