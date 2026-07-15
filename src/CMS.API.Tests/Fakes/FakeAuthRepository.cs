using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="IAuthRepository"/> so login tests run without a live SQL Server.</summary>
public class FakeAuthRepository : IAuthRepository
{
    /// <summary>Signing key — long enough (&gt;= 256 bits) for HS256.</summary>
    public const string SigningKey = "test-signing-key-that-is-at-least-32-bytes-long-for-hmac-sha256";

    public const string HelenPassword = "secret123";
    public const string MilesPassword = "miles-pass";
    public const string NinaPassword = "nina-pass";

    /// <summary>The system default password the reset operation restores accounts to (stands in for
    /// <c>SysConfig['appConfig'].defaultPassword</c>). Tests assert the reset hash equals SHA256(this).</summary>
    public const string DefaultPassword = "Default-P@ss1";

    private readonly List<AuthCredential> _credentials =
    [
        // Active Admin — can obtain a token and is authorized to reset other users' passwords.
        new AuthCredential
        {
            UserId = "helen",
            UserName = "Helen Wang",
            IsActive = true,
            PasswordHash = PasswordHasher.Sha256(HelenPassword),
            PasswordUpdatedTime = new DateTime(2026, 1, 1, 9, 0, 0),
            RoleIds = ["Admin", "User"]
        },
        // Correct password but inactive — login must still be rejected.
        new AuthCredential
        {
            UserId = "miles",
            UserName = "Miles Sun",
            IsActive = false,
            PasswordHash = PasswordHasher.Sha256(MilesPassword),
            PasswordUpdatedTime = new DateTime(2026, 1, 1, 9, 0, 0),
            RoleIds = ["User"]
        },
        // Active non-Admin — can obtain a token but must be forbidden (403) from resetting passwords.
        new AuthCredential
        {
            UserId = "nina",
            UserName = "Nina Lee",
            IsActive = true,
            PasswordHash = PasswordHasher.Sha256(NinaPassword),
            PasswordUpdatedTime = new DateTime(2026, 1, 1, 9, 0, 0),
            RoleIds = ["User"]
        }
    ];

    public Task<AuthCredential?> GetCredentialAsync(string userId)
        => Task.FromResult(_credentials.FirstOrDefault(c => c.UserId == userId));

    public Task<string> GetSigningKeyAsync() => Task.FromResult(SigningKey);

    public Task UpdateUserNameAsync(string userId, string userName)
    {
        var credential = _credentials.FirstOrDefault(c => c.UserId == userId);
        if (credential is not null)
            credential.UserName = userName;
        return Task.CompletedTask;
    }

    public Task UpdatePasswordAsync(string userId, string newPasswordHash)
    {
        var credential = _credentials.FirstOrDefault(c => c.UserId == userId);
        if (credential is not null)
        {
            credential.PasswordHash = newPasswordHash;
            credential.PasswordUpdatedTime = DateTime.Now;
        }
        return Task.CompletedTask;
    }

    public Task<bool> ResetPasswordToDefaultAsync(string userId)
    {
        var credential = _credentials.FirstOrDefault(c => c.UserId == userId);
        if (credential is null)
            return Task.FromResult(false);

        // Mirror the real repository: store SHA256 of the (config) default and stamp the update time.
        credential.PasswordHash = PasswordHasher.Sha256(DefaultPassword);
        credential.PasswordUpdatedTime = DateTime.Now;
        return Task.FromResult(true);
    }
}
