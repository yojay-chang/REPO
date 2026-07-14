using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppRoleRepository
{
    Task<IEnumerable<AppRole>> GetAllAsync();
    Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query);
    Task<AppRole?> GetByIdAsync(string roleId);
    Task<bool> ExistsAsync(string roleId);
    Task<string> CreateAsync(AppRoleRequest request);
    Task<bool> UpdateAsync(AppRoleRequest request);
    Task<bool> DeleteAsync(string roleId);
}
