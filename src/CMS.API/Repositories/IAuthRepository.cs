using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAuthRepository
{
    /// <summary>
    /// Load the login credential (incl. PasswordHash and assigned roles) for a user, or null if no
    /// such <c>UserId</c> exists. PasswordHash is used only for server-side verification.
    /// </summary>
    Task<AuthCredential?> GetCredentialAsync(string userId);

    /// <summary>
    /// The JWT signing secret — the <c>symmetricSecurityKey</c> property of the JSON stored in
    /// <c>SysConfig</c> where <c>configKey = 'appConfig'</c>. Read at runtime; never hard-coded.
    /// </summary>
    Task<string> GetSigningKeyAsync();

    /// <summary>
    /// Update only the <c>UserName</c> of <paramref name="userId"/> (self-service profile edit).
    /// The caller is responsible for supplying the authenticated user's id — never a client-provided one.
    /// </summary>
    Task UpdateUserNameAsync(string userId, string userName);

    /// <summary>
    /// Set the <c>PasswordHash</c> of <paramref name="userId"/> to <paramref name="newPasswordHash"/>
    /// and stamp <c>PasswordUpdatedTime</c> to now. The caller has already verified the current password
    /// and must supply the authenticated user's id (from the JWT), never a client-provided one.
    /// </summary>
    Task UpdatePasswordAsync(string userId, string newPasswordHash);

    /// <summary>
    /// Reset <paramref name="userId"/>'s password to the system default: read
    /// <c>SysConfig['appConfig'].defaultPassword</c> at runtime, set <c>PasswordHash = SHA256(default)</c>
    /// and stamp <c>PasswordUpdatedTime</c> to now. Returns <c>false</c> if no such user exists. This is an
    /// Admin-only operation; the plaintext default is used only for hashing and never leaves the backend.
    /// </summary>
    Task<bool> ResetPasswordToDefaultAsync(string userId);
}
