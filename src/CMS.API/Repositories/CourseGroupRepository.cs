using System.Data;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class CourseGroupRepository : ICourseGroupRepository
{
    private const string TableName = "CourseGroup";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRowAuditWriter _audit;

    public CourseGroupRepository(IDbConnectionFactory connectionFactory, IRowAuditWriter audit)
    {
        _connectionFactory = connectionFactory;
        _audit = audit;
    }

    private const string SelectColumns = @"
        SELECT cg.pkid, cg.Description
        FROM CourseGroup cg";

    public async Task<IEnumerable<CourseGroup>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<CourseGroup>($"{SelectColumns} ORDER BY cg.pkid DESC");
    }

    public async Task<IEnumerable<CourseGroup>> QueryAsync(CourseGroupQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("cg.Description LIKE @Keyword");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY cg.pkid DESC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<CourseGroup>(sql, parameters);
    }

    public async Task<CourseGroup?> GetByIdAsync(short pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CourseGroup>(
            $"{SelectColumns} WHERE cg.pkid = @Pkid", new { Pkid = pkid });
    }

    public async Task<short> CreateAsync(CourseGroupRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var pkid = await db.ExecuteScalarAsync<short>(
            @"INSERT INTO CourseGroup (Description)
              VALUES (@Description);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            new
            {
                request.Description
            }, tx);

        var created = await ReadForAuditAsync(db, tx, pkid);
        await _audit.LogInsertAsync(db, tx, TableName, created);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request)
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
            @"UPDATE CourseGroup
                 SET Description = @Description
               WHERE pkid = @Pkid",
            new
            {
                request.Pkid,
                request.Description
            }, tx);

        var after = await ReadForAuditAsync(db, tx, request.Pkid);
        await _audit.LogUpdateAsync(db, tx, TableName, before, after);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(short pkid)
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

        await db.ExecuteAsync("DELETE FROM CourseGroup WHERE pkid = @Pkid", new { Pkid = pkid }, tx);
        await _audit.LogDeleteAsync(db, tx, TableName, before);

        tx.Commit();
        return true;
    }

    /// <summary>Read the current row (base columns only) on the caller's connection/transaction for auditing.</summary>
    private static Task<CourseGroup?> ReadForAuditAsync(IDbConnection db, IDbTransaction tx, short pkid) =>
        db.QuerySingleOrDefaultAsync<CourseGroup>(
            $"{SelectColumns} WHERE cg.pkid = @Pkid", new { Pkid = pkid }, tx);
}
