using System.Data;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class AppRoleRepository : IAppRoleRepository
{
    private const string TableName = "AppRole";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRowAuditWriter _audit;

    public AppRoleRepository(IDbConnectionFactory connectionFactory, IRowAuditWriter audit)
    {
        _connectionFactory = connectionFactory;
        _audit = audit;
    }

    private const string SelectColumns = @"
        SELECT r.pkid, r.RoleId, r.RoleName, r.PermissionLevel, r.Description,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.RoleId = r.RoleId) AS UserCount
        FROM AppRole r";

    // Base columns only (no user-count subquery, no n-n list) — the shape compared for audit so the
    // changed-column list reflects only the real AppRole columns.
    private const string AuditSelectColumns = @"
        SELECT r.pkid, r.RoleId, r.RoleName, r.PermissionLevel, r.Description
        FROM AppRole r";

    public async Task<IEnumerable<AppRole>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppRole>($"{SelectColumns} ORDER BY r.RoleId ASC");
    }

    public async Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(r.RoleId LIKE @Keyword OR r.RoleName LIKE @Keyword OR r.Description LIKE @Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.PermissionLevelFrom.HasValue)
        {
            where.Add("r.PermissionLevel >= @PermissionLevelFrom");
            parameters.Add("PermissionLevelFrom", query.PermissionLevelFrom.Value);
        }

        if (query.PermissionLevelTo.HasValue)
        {
            where.Add("r.PermissionLevel <= @PermissionLevelTo");
            parameters.Add("PermissionLevelTo", query.PermissionLevelTo.Value);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY r.RoleId ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppRole>(sql, parameters);
    }

    public async Task<AppRole?> GetByIdAsync(string roleId)
    {
        using var db = _connectionFactory.CreateConnection();
        var role = await db.QuerySingleOrDefaultAsync<AppRole>(
            $"{SelectColumns} WHERE r.RoleId = @RoleId", new { RoleId = roleId });

        if (role is null)
            return null;

        var userIds = await db.QueryAsync<string>(
            "SELECT UserId FROM AppUserRole WHERE RoleId = @RoleId ORDER BY UserId", new { RoleId = roleId });
        role.UserIds = userIds.ToList();
        return role;
    }

    public async Task<bool> ExistsAsync(string roleId)
    {
        using var db = _connectionFactory.CreateConnection();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM AppRole WHERE RoleId = @RoleId", new { RoleId = roleId });
        return count > 0;
    }

    public async Task<string> CreateAsync(AppRoleRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        await db.ExecuteAsync(
            @"INSERT INTO AppRole (RoleId, RoleName, PermissionLevel, Description)
              VALUES (@RoleId, @RoleName, @PermissionLevel, @Description)",
            new
            {
                request.RoleId,
                request.RoleName,
                request.PermissionLevel,
                request.Description
            }, tx);

        await SyncUsersAsync(db, tx, request.RoleId, request.UserIds);

        var created = await ReadForAuditAsync(db, tx, request.RoleId);
        await _audit.LogInsertAsync(db, tx, TableName, created);

        tx.Commit();
        return request.RoleId;
    }

    public async Task<bool> UpdateAsync(AppRoleRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await ReadForAuditAsync(db, tx, request.RoleId);

        var affected = await db.ExecuteAsync(
            @"UPDATE AppRole
                 SET RoleName = @RoleName,
                     PermissionLevel = @PermissionLevel,
                     Description = @Description
               WHERE RoleId = @RoleId",
            new
            {
                request.RoleId,
                request.RoleName,
                request.PermissionLevel,
                request.Description
            }, tx);

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncUsersAsync(db, tx, request.RoleId, request.UserIds);

        var after = await ReadForAuditAsync(db, tx, request.RoleId);
        await _audit.LogUpdateAsync(db, tx, TableName, before, after);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string roleId)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await ReadForAuditAsync(db, tx, roleId);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE RoleId = @RoleId", new { RoleId = roleId }, tx);
        await db.ExecuteAsync("DELETE FROM AppRole WHERE RoleId = @RoleId", new { RoleId = roleId }, tx);

        await _audit.LogDeleteAsync(db, tx, TableName, before);

        tx.Commit();
        return true;
    }

    /// <summary>Read the current row (base columns only) on the caller's connection/transaction for auditing.</summary>
    private static Task<AppRole?> ReadForAuditAsync(IDbConnection db, IDbTransaction tx, string roleId) =>
        db.QuerySingleOrDefaultAsync<AppRole>(
            $"{AuditSelectColumns} WHERE r.RoleId = @RoleId", new { RoleId = roleId }, tx);

    /// <summary>Delete-then-reinsert the AppUserRole rows for a role (n-n sync).</summary>
    private static async Task SyncUsersAsync(IDbConnection db, IDbTransaction tx, string roleId, List<string> userIds)
    {
        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE RoleId = @RoleId", new { RoleId = roleId }, tx);

        var distinct = userIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await db.ExecuteAsync(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(userId => new { UserId = userId, RoleId = roleId }), tx);
    }
}
