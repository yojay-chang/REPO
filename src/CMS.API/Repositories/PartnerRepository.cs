using System.Data;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private const string TableName = "Partner";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRowAuditWriter _audit;

    public PartnerRepository(IDbConnectionFactory connectionFactory, IRowAuditWriter audit)
    {
        _connectionFactory = connectionFactory;
        _audit = audit;
    }

    private const string SelectColumns = @"
        SELECT p.pkid, p.Name, p.AppKey, p.NameOnPartnerMenu, p.NameOnCourseDetailPage,
               p.DisplayOrder, p.ImageFilename
        FROM Partner p";

    public async Task<IEnumerable<Partner>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<Partner>($"{SelectColumns} ORDER BY p.DisplayOrder ASC");
    }

    public async Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add(@"(p.Name LIKE @Keyword
                        OR p.AppKey LIKE @Keyword
                        OR p.NameOnPartnerMenu LIKE @Keyword
                        OR p.NameOnCourseDetailPage LIKE @Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY p.DisplayOrder ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<Partner>(sql, parameters);
    }

    public async Task<Partner?> GetByIdAsync(short pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Partner>(
            $"{SelectColumns} WHERE p.pkid = @Pkid", new { Pkid = pkid });
    }

    public async Task<short> CreateAsync(PartnerRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var pkid = await db.ExecuteScalarAsync<short>(
            @"INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
              VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            new
            {
                request.Name,
                request.AppKey,
                request.NameOnPartnerMenu,
                request.NameOnCourseDetailPage,
                request.DisplayOrder,
                request.ImageFilename
            }, tx);

        var created = await ReadForAuditAsync(db, tx, pkid);
        await _audit.LogInsertAsync(db, tx, TableName, created);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(PartnerRequest request)
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
            @"UPDATE Partner
                 SET Name = @Name,
                     AppKey = @AppKey,
                     NameOnPartnerMenu = @NameOnPartnerMenu,
                     NameOnCourseDetailPage = @NameOnCourseDetailPage,
                     DisplayOrder = @DisplayOrder,
                     ImageFilename = @ImageFilename
               WHERE pkid = @Pkid",
            new
            {
                request.Pkid,
                request.Name,
                request.AppKey,
                request.NameOnPartnerMenu,
                request.NameOnCourseDetailPage,
                request.DisplayOrder,
                request.ImageFilename
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

        await db.ExecuteAsync("DELETE FROM Partner WHERE pkid = @Pkid", new { Pkid = pkid }, tx);
        await _audit.LogDeleteAsync(db, tx, TableName, before);

        tx.Commit();
        return true;
    }

    /// <summary>Read the current row (base columns only) on the caller's connection/transaction for auditing.</summary>
    private static Task<Partner?> ReadForAuditAsync(IDbConnection db, IDbTransaction tx, short pkid) =>
        db.QuerySingleOrDefaultAsync<Partner>(
            $"{SelectColumns} WHERE p.pkid = @Pkid", new { Pkid = pkid }, tx);
}
