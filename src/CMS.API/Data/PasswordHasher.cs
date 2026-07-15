using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Data;

/// <summary>SHA-256 password hashing (used to store AppUser.PasswordHash on create / reset).</summary>
public static class PasswordHasher
{
    /// <summary>Returns the lowercase hex SHA-256 hash of <paramref name="value"/>.</summary>
    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
