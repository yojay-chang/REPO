using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="ICourseGroupRepository"/> so controller tests run without a live SQL Server.
/// Mimics the smallint IDENTITY PK by assigning the next pkid (max + 1) on create.</summary>
public class FakeCourseGroupRepository : ICourseGroupRepository
{
    private readonly List<CourseGroup> _courseGroups = [];

    public FakeCourseGroupRepository()
    {
        _courseGroups.Add(new CourseGroup { Pkid = 1, Description = "資料庫" });
        _courseGroups.Add(new CourseGroup { Pkid = 2, Description = "網路管理" });
        _courseGroups.Add(new CourseGroup { Pkid = 3, Description = "雲端服務" });
    }

    public Task<IEnumerable<CourseGroup>> GetAllAsync()
        => Task.FromResult(_courseGroups.OrderByDescending(cg => cg.Pkid).Select(Clone).AsEnumerable());

    public Task<IEnumerable<CourseGroup>> QueryAsync(CourseGroupQuery query)
    {
        IEnumerable<CourseGroup> result = _courseGroups;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(cg =>
                cg.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(result.OrderByDescending(cg => cg.Pkid).Select(Clone).AsEnumerable());
    }

    public Task<CourseGroup?> GetByIdAsync(short pkid)
    {
        var courseGroup = _courseGroups.FirstOrDefault(cg => cg.Pkid == pkid);
        return Task.FromResult(courseGroup is null ? null : Clone(courseGroup));
    }

    public Task<short> CreateAsync(CourseGroupRequest request)
    {
        var nextPkid = (short)(_courseGroups.Count == 0 ? 1 : _courseGroups.Max(cg => cg.Pkid) + 1);
        _courseGroups.Add(new CourseGroup
        {
            Pkid = nextPkid,
            Description = request.Description
        });
        return Task.FromResult(nextPkid);
    }

    public Task<bool> UpdateAsync(CourseGroupRequest request)
    {
        var courseGroup = _courseGroups.FirstOrDefault(cg => cg.Pkid == request.Pkid);
        if (courseGroup is null)
            return Task.FromResult(false);

        courseGroup.Description = request.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid)
    {
        var removed = _courseGroups.RemoveAll(cg => cg.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    private static CourseGroup Clone(CourseGroup cg) => new()
    {
        Pkid = cg.Pkid,
        Description = cg.Description
    };
}
