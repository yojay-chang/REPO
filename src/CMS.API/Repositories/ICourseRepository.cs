using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync();
    Task<IEnumerable<Course>> QueryAsync(CourseQuery query);
    Task<Course?> GetByIdAsync(int pkid);
    Task<int> CreateAsync(CourseRequest request);
    Task<bool> UpdateAsync(CourseRequest request);
    Task<bool> DeleteAsync(int pkid);
}
