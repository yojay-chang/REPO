using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="IAppUserRepository"/> so controller tests run without a live SQL Server.</summary>
public class FakeAppUserRepository : IAppUserRepository
{
    private readonly List<AppUser> _users = [];
    private int _nextPkid = 1;

    public FakeAppUserRepository()
    {
        Seed(new AppUser
        {
            UserId = "helen",
            UserName = "Helen Wang",
            IsActive = true,
            PasswordUpdatedTime = new DateTime(2026, 1, 1, 9, 0, 0),
            RoleIds = ["Admin", "User"]
        });
        Seed(new AppUser
        {
            UserId = "miles",
            UserName = "Miles Sun",
            IsActive = false,
            PasswordUpdatedTime = null,
            RoleIds = ["User"]
        });
    }

    private void Seed(AppUser user)
    {
        user.Pkid = _nextPkid++;
        user.RoleCount = user.RoleIds.Count;
        _users.Add(user);
    }

    public Task<IEnumerable<AppUser>> GetAllAsync()
        => Task.FromResult(_users.OrderBy(u => u.UserId).Select(Clone).AsEnumerable());

    public Task<IEnumerable<AppUser>> QueryAsync(AppUserQuery query)
    {
        IEnumerable<AppUser> result = _users;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(u =>
                u.UserId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                u.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsActive.HasValue)
            result = result.Where(u => u.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.RoleId))
            result = result.Where(u => u.RoleIds.Contains(query.RoleId.Trim()));

        return Task.FromResult(result.OrderBy(u => u.UserId).Select(Clone).AsEnumerable());
    }

    public Task<AppUser?> GetByIdAsync(string userId)
    {
        var user = _users.FirstOrDefault(u => u.UserId == userId);
        return Task.FromResult(user is null ? null : Clone(user));
    }

    public Task<bool> ExistsAsync(string userId)
        => Task.FromResult(_users.Any(u => u.UserId == userId));

    public Task<string> CreateAsync(AppUserRequest request)
    {
        Seed(new AppUser
        {
            UserId = request.UserId,
            UserName = request.UserName,
            IsActive = request.IsActive,
            PasswordUpdatedTime = new DateTime(2026, 7, 15, 0, 0, 0),
            RoleIds = [.. request.RoleIds]
        });
        return Task.FromResult(request.UserId);
    }

    public Task<bool> UpdateAsync(AppUserRequest request)
    {
        var user = _users.FirstOrDefault(u => u.UserId == request.UserId);
        if (user is null)
            return Task.FromResult(false);

        user.UserName = request.UserName;
        user.IsActive = request.IsActive;
        user.RoleIds = [.. request.RoleIds];
        user.RoleCount = user.RoleIds.Count;
        // PasswordUpdatedTime intentionally left unchanged by update.
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string userId)
    {
        var removed = _users.RemoveAll(u => u.UserId == userId);
        return Task.FromResult(removed > 0);
    }

    private static AppUser Clone(AppUser u) => new()
    {
        Pkid = u.Pkid,
        UserId = u.UserId,
        UserName = u.UserName,
        IsActive = u.IsActive,
        PasswordUpdatedTime = u.PasswordUpdatedTime,
        RoleCount = u.RoleCount,
        RoleIds = [.. u.RoleIds]
    };
}
