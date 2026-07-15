using System.Data;
using System.Text.Json;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthCredential?> GetCredentialAsync(string userId)
    {
        using var db = _connectionFactory.CreateConnection();

        // PasswordHash is selected here for verification only — it is never returned to the caller.
        var credential = await db.QuerySingleOrDefaultAsync<AuthCredential>(
            "SELECT UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime FROM AppUser WHERE UserId = @UserId",
            new { UserId = userId });

        if (credential is null)
            return null;

        var roleIds = await db.QueryAsync<string>(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId",
            new { UserId = userId });
        credential.RoleIds = roleIds.ToList();

        return credential;
    }

    /// <summary>Read SysConfig['appConfig'] (a JSON object) and extract the <c>symmetricSecurityKey</c> property.</summary>
    public async Task<string> GetSigningKeyAsync()
    {
        using var db = _connectionFactory.CreateConnection();

        var json = await db.ExecuteScalarAsync<string?>(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'");

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("SysConfig 'appConfig' is missing.");

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("symmetricSecurityKey", out var prop) ||
            prop.ValueKind != JsonValueKind.String ||
            string.IsNullOrEmpty(prop.GetString()))
        {
            throw new InvalidOperationException("SysConfig 'appConfig' has no 'symmetricSecurityKey' value.");
        }

        return prop.GetString()!;
    }

    public async Task UpdateUserNameAsync(string userId, string userName)
    {
        using var db = _connectionFactory.CreateConnection();

        await db.ExecuteAsync(
            "UPDATE AppUser SET UserName = @UserName WHERE UserId = @UserId",
            new { UserId = userId, UserName = userName });
    }

    public async Task UpdatePasswordAsync(string userId, string newPasswordHash)
    {
        using var db = _connectionFactory.CreateConnection();

        await db.ExecuteAsync(
            @"UPDATE AppUser
                 SET PasswordHash = @PasswordHash,
                     PasswordUpdatedTime = @PasswordUpdatedTime
               WHERE UserId = @UserId",
            new
            {
                UserId = userId,
                PasswordHash = newPasswordHash,
                PasswordUpdatedTime = DateTime.Now
            });
    }

    public async Task<bool> ResetPasswordToDefaultAsync(string userId)
    {
        using var db = _connectionFactory.CreateConnection();

        // Read the default password at runtime and store only its hash — the plaintext never leaves here.
        var passwordHash = PasswordHasher.Sha256(await GetDefaultPasswordAsync(db));

        var affected = await db.ExecuteAsync(
            @"UPDATE AppUser
                 SET PasswordHash = @PasswordHash,
                     PasswordUpdatedTime = @PasswordUpdatedTime
               WHERE UserId = @UserId",
            new
            {
                UserId = userId,
                PasswordHash = passwordHash,
                PasswordUpdatedTime = DateTime.Now
            });

        return affected > 0;
    }

    /// <summary>Read SysConfig['appConfig'] (a JSON object) and extract the <c>defaultPassword</c> property.</summary>
    private static async Task<string> GetDefaultPasswordAsync(IDbConnection db)
    {
        var json = await db.ExecuteScalarAsync<string?>(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'");

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
}
