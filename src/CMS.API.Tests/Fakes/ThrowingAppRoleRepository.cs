using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// An <see cref="IAppRoleRepository"/> whose every operation throws — stands in for a repository/Dapper
/// failure so the global exception-handling middleware can be exercised end-to-end. The thrown message
/// deliberately carries SQL- and connection-looking text so a test can assert none of it leaks to the client.
/// </summary>
public class ThrowingAppRoleRepository : IAppRoleRepository
{
    /// <summary>Sensitive-looking detail that must NEVER appear in the client-facing response.</summary>
    public const string SensitiveMessage =
        "Server=sql-prod;Password=s3cr3t; SELECT * FROM AppRole -- boom at CMS.API.Repositories.AppRoleRepository";

    private static InvalidOperationException Boom() => new(SensitiveMessage);

    public Task<IEnumerable<AppRole>> GetAllAsync() => throw Boom();
    public Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query) => throw Boom();
    public Task<AppRole?> GetByIdAsync(string roleId) => throw Boom();
    public Task<bool> ExistsAsync(string roleId) => throw Boom();
    public Task<string> CreateAsync(AppRoleRequest request) => throw Boom();
    public Task<bool> UpdateAsync(AppRoleRequest request) => throw Boom();
    public Task<bool> DeleteAsync(string roleId) => throw Boom();
}
