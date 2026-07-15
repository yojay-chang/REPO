using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppUserRepository
{
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<IEnumerable<AppUser>> QueryAsync(AppUserQuery query);
    Task<AppUser?> GetByIdAsync(string userId);
    Task<bool> ExistsAsync(string userId);
    Task<string> CreateAsync(AppUserRequest request);
    Task<bool> UpdateAsync(AppUserRequest request);
    Task<bool> DeleteAsync(string userId);
}
