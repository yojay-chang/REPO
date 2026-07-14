using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="IAppRoleRepository"/> so controller tests run without a live SQL Server.</summary>
public class FakeAppRoleRepository : IAppRoleRepository
{
    private readonly List<AppRole> _roles = [];
    private int _nextPkid = 1;

    public FakeAppRoleRepository()
    {
        Seed(new AppRole
        {
            RoleId = "Admin",
            RoleName = "Administrator",
            PermissionLevel = 1,
            Description = "系統管理員",
            UserIds = ["helen", "Jenny_Tsao", "miles"]
        });
        Seed(new AppRole
        {
            RoleId = "User",
            RoleName = "User",
            PermissionLevel = 100,
            Description = "一般使用者",
            UserIds = ["u1", "u2", "u3", "u4", "u5", "u6", "u7", "u8", "u9"]
        });
    }

    private void Seed(AppRole role)
    {
        role.Pkid = _nextPkid++;
        role.UserCount = role.UserIds.Count;
        _roles.Add(role);
    }

    public Task<IEnumerable<AppRole>> GetAllAsync()
        => Task.FromResult(_roles.OrderBy(r => r.RoleId).Select(Clone).AsEnumerable());

    public Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query)
    {
        IEnumerable<AppRole> result = _roles;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(r =>
                r.RoleId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                r.RoleName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (r.Description ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PermissionLevelFrom.HasValue)
            result = result.Where(r => r.PermissionLevel >= query.PermissionLevelFrom.Value);

        if (query.PermissionLevelTo.HasValue)
            result = result.Where(r => r.PermissionLevel <= query.PermissionLevelTo.Value);

        return Task.FromResult(result.OrderBy(r => r.RoleId).Select(Clone).AsEnumerable());
    }

    public Task<AppRole?> GetByIdAsync(string roleId)
    {
        var role = _roles.FirstOrDefault(r => r.RoleId == roleId);
        return Task.FromResult(role is null ? null : Clone(role));
    }

    public Task<bool> ExistsAsync(string roleId)
        => Task.FromResult(_roles.Any(r => r.RoleId == roleId));

    public Task<string> CreateAsync(AppRoleRequest request)
    {
        Seed(new AppRole
        {
            RoleId = request.RoleId,
            RoleName = request.RoleName,
            PermissionLevel = request.PermissionLevel,
            Description = request.Description,
            UserIds = [.. request.UserIds]
        });
        return Task.FromResult(request.RoleId);
    }

    public Task<bool> UpdateAsync(AppRoleRequest request)
    {
        var role = _roles.FirstOrDefault(r => r.RoleId == request.RoleId);
        if (role is null)
            return Task.FromResult(false);

        role.RoleName = request.RoleName;
        role.PermissionLevel = request.PermissionLevel;
        role.Description = request.Description;
        role.UserIds = [.. request.UserIds];
        role.UserCount = role.UserIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string roleId)
    {
        var removed = _roles.RemoveAll(r => r.RoleId == roleId);
        return Task.FromResult(removed > 0);
    }

    private static AppRole Clone(AppRole r) => new()
    {
        Pkid = r.Pkid,
        RoleId = r.RoleId,
        RoleName = r.RoleName,
        PermissionLevel = r.PermissionLevel,
        Description = r.Description,
        UserCount = r.UserCount,
        UserIds = [.. r.UserIds]
    };
}
