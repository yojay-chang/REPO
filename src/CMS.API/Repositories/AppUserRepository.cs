using System.Data;
using System.Text.Json;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AppUserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // PasswordHash is intentionally NOT selected — it never leaves the backend.
    private const string SelectColumns = @"
        SELECT u.pkid, u.UserId, u.UserName, u.IsActive, u.PasswordUpdatedTime,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
        FROM AppUser u";

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppUser>($"{SelectColumns} ORDER BY u.UserId ASC");
    }

    public async Task<IEnumerable<AppUser>> QueryAsync(AppUserQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(u.UserId LIKE @Keyword OR u.UserName LIKE @Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.IsActive.HasValue)
        {
            where.Add("u.IsActive = @IsActive");
            parameters.Add("IsActive", query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RoleId))
        {
            where.Add("EXISTS (SELECT 1 FROM AppUserRole ur WHERE ur.UserId = u.UserId AND ur.RoleId = @RoleId)");
            parameters.Add("RoleId", query.RoleId.Trim());
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY u.UserId ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppUser>(sql, parameters);
    }

    public async Task<AppUser?> GetByIdAsync(string userId)
    {
        using var db = _connectionFactory.CreateConnection();
        var user = await db.QuerySingleOrDefaultAsync<AppUser>(
            $"{SelectColumns} WHERE u.UserId = @UserId", new { UserId = userId });

        if (user is null)
            return null;

        var roleIds = await db.QueryAsync<string>(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId", new { UserId = userId });
        user.RoleIds = roleIds.ToList();
        return user;
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        using var db = _connectionFactory.CreateConnection();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM AppUser WHERE UserId = @UserId", new { UserId = userId });
        return count > 0;
    }

    public async Task<string> CreateAsync(AppUserRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var passwordHash = PasswordHasher.Sha256(GetDefaultPassword(db, tx));

        await db.ExecuteAsync(
            @"INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
              VALUES (@UserId, @UserName, @IsActive, @PasswordHash, @PasswordUpdatedTime)",
            new
            {
                request.UserId,
                request.UserName,
                request.IsActive,
                PasswordHash = passwordHash,
                PasswordUpdatedTime = DateTime.Now
            }, tx);

        await SyncRolesAsync(db, tx, request.UserId, request.RoleIds);

        tx.Commit();
        return request.UserId;
    }

    public async Task<bool> UpdateAsync(AppUserRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        // PasswordHash / PasswordUpdatedTime are deliberately left untouched by update.
        var affected = await db.ExecuteAsync(
            @"UPDATE AppUser
                 SET UserName = @UserName,
                     IsActive = @IsActive
               WHERE UserId = @UserId",
            new
            {
                request.UserId,
                request.UserName,
                request.IsActive
            }, tx);

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncRolesAsync(db, tx, request.UserId, request.RoleIds);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE UserId = @UserId", new { UserId = userId }, tx);
        var affected = await db.ExecuteAsync("DELETE FROM AppUser WHERE UserId = @UserId", new { UserId = userId }, tx);

        tx.Commit();
        return affected > 0;
    }

    /// <summary>Read SysConfig['appConfig'] (a JSON object) and extract the <c>defaultPassword</c> property.</summary>
    private static string GetDefaultPassword(IDbConnection db, IDbTransaction tx)
    {
        var json = db.ExecuteScalar<string?>(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'", transaction: tx);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("SysConfig 'appConfig' is missing.");

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("defaultPassword", out var prop) ||
            prop.ValueKind != JsonValueKind.String ||
            string.IsNullOrEmpty(prop.GetString()))
        {
            throw new InvalidOperationException("SysConfig 'appConfig' has no 'defaultPassword' value.");
        }

        return prop.GetString()!;
    }

    /// <summary>Delete-then-reinsert the AppUserRole rows for a user (n-n sync).</summary>
    private static async Task SyncRolesAsync(IDbConnection db, IDbTransaction tx, string userId, List<string> roleIds)
    {
        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE UserId = @UserId", new { UserId = userId }, tx);

        var distinct = roleIds.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await db.ExecuteAsync(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(roleId => new { UserId = userId, RoleId = roleId }), tx);
    }
}
