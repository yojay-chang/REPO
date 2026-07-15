using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="ICourseRepository"/> so controller tests run without a live SQL Server.
/// Mimics the int IDENTITY PK by assigning the next pkid (max + 1) on create, stores the N-N pkid
/// lists in-memory, and resolves the JOINed FK labels from small static parent maps that mirror
/// <see cref="FakeLookupRepository"/>.</summary>
public class FakeCourseRepository : ICourseRepository
{
    private static readonly Dictionary<short, string> Partners = new()
    {
        [1] = "微軟", [2] = "思科", [3] = "紅帽",
    };
    private static readonly Dictionary<short, string> CourseGroups = new()
    {
        [1] = "資料庫", [2] = "網路管理", [3] = "雲端服務",
    };
    private static readonly Dictionary<byte, string> PublishStatuses = new()
    {
        [1] = "草稿", [2] = "已發布", [3] = "已停用",
    };

    private readonly List<Course> _courses = [];

    public FakeCourseRepository()
    {
        _courses.Add(new Course
        {
            Pkid = 1, Title = "Azure 基礎", OfficialTitle = "Microsoft Azure Fundamentals",
            CourseId = "AZ-900", ProdCourseId = "PRD-AZ900", FriendlyUrl = "azure-900", DisplayOrder = 1,
            PartnerPkid = 1, CourseGroupPkid = 3, PublishStatusPkid = 2,
            ScheduleOn = new DateOnly(2026, 1, 1), ScheduleOff = new DateOnly(2036, 1, 1),
            Hour = 14, ListPrice = 12000m, LearningCredit = 3.0m, CanRepeat = true,
            CertificationPkids = [1], JobCategoryPkids = [1],
        });
        _courses.Add(new Course
        {
            Pkid = 2, Title = "CCNA 網路", CourseId = "CCNA-200", ProdCourseId = "PRD-CCNA",
            FriendlyUrl = "ccna-200", DisplayOrder = 2,
            PartnerPkid = 2, CourseGroupPkid = null, PublishStatusPkid = 1,
            ScheduleOn = new DateOnly(2026, 2, 1), ScheduleOff = new DateOnly(2036, 2, 1),
            Hour = 40, ListPrice = 30000m, LearningCredit = 5.0m, CanRepeat = false,
            CertificationPkids = [], JobCategoryPkids = [2],
        });
    }

    public Task<IEnumerable<Course>> GetAllAsync()
        => Task.FromResult(_courses.OrderBy(c => c.DisplayOrder).Select(Clone).AsEnumerable());

    public Task<IEnumerable<Course>> QueryAsync(CourseQuery query)
    {
        IEnumerable<Course> result = _courses;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(c =>
                c.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (c.OfficialTitle?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)
                || c.CourseId.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || c.ProdCourseId.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || c.FriendlyUrl.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PartnerPkid.HasValue)
            result = result.Where(c => c.PartnerPkid == query.PartnerPkid.Value);
        if (query.CourseGroupPkid.HasValue)
            result = result.Where(c => c.CourseGroupPkid == query.CourseGroupPkid.Value);
        if (query.PublishStatusPkid.HasValue)
            result = result.Where(c => c.PublishStatusPkid == query.PublishStatusPkid.Value);
        if (query.ScheduleOnFrom.HasValue)
            result = result.Where(c => c.ScheduleOn >= query.ScheduleOnFrom.Value);
        if (query.ScheduleOnTo.HasValue)
            result = result.Where(c => c.ScheduleOn <= query.ScheduleOnTo.Value);
        if (query.ScheduleOffFrom.HasValue)
            result = result.Where(c => c.ScheduleOff >= query.ScheduleOffFrom.Value);
        if (query.ScheduleOffTo.HasValue)
            result = result.Where(c => c.ScheduleOff <= query.ScheduleOffTo.Value);
        if (query.CanRepeat.HasValue)
            result = result.Where(c => c.CanRepeat == query.CanRepeat.Value);

        return Task.FromResult(result.OrderBy(c => c.DisplayOrder).Select(Clone).AsEnumerable());
    }

    public Task<Course?> GetByIdAsync(int pkid)
    {
        var course = _courses.FirstOrDefault(c => c.Pkid == pkid);
        return Task.FromResult(course is null ? null : Clone(course));
    }

    public Task<int> CreateAsync(CourseRequest request)
    {
        var nextPkid = _courses.Count == 0 ? 1 : _courses.Max(c => c.Pkid) + 1;
        _courses.Add(FromRequest(request, nextPkid));
        return Task.FromResult(nextPkid);
    }

    public Task<bool> UpdateAsync(CourseRequest request)
    {
        var index = _courses.FindIndex(c => c.Pkid == request.Pkid);
        if (index < 0)
            return Task.FromResult(false);

        _courses[index] = FromRequest(request, request.Pkid);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid)
    {
        var removed = _courses.RemoveAll(c => c.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    private static Course FromRequest(CourseRequest r, int pkid) => new()
    {
        Pkid = pkid,
        Title = r.Title,
        OfficialTitle = r.OfficialTitle,
        CourseId = r.CourseId,
        ProdCourseId = r.ProdCourseId,
        FriendlyUrl = r.FriendlyUrl,
        DisplayOrder = r.DisplayOrder,
        PartnerPkid = r.PartnerPkid,
        CourseGroupPkid = r.CourseGroupPkid,
        PublishStatusPkid = r.PublishStatusPkid,
        ScheduleOn = r.ScheduleOn,
        ScheduleOff = r.ScheduleOff,
        Hour = r.Hour,
        ListPrice = r.ListPrice,
        LearningCredit = r.LearningCredit,
        Material = r.Material,
        Objective = r.Objective,
        Target = r.Target,
        Prerequisites = r.Prerequisites,
        Outline = r.Outline,
        TowardCertOrExam = r.TowardCertOrExam,
        Note = r.Note,
        OtherInfo = r.OtherInfo,
        CanRepeat = r.CanRepeat,
        CertificationPkids = r.CertificationPkids.Distinct().ToList(),
        JobCategoryPkids = r.JobCategoryPkids.Distinct().ToList(),
    };

    /// <summary>Copy with the JOINed FK labels resolved (mirrors the repository SELECT).</summary>
    private static Course Clone(Course c) => new()
    {
        Pkid = c.Pkid,
        Title = c.Title,
        OfficialTitle = c.OfficialTitle,
        CourseId = c.CourseId,
        ProdCourseId = c.ProdCourseId,
        FriendlyUrl = c.FriendlyUrl,
        DisplayOrder = c.DisplayOrder,
        PartnerPkid = c.PartnerPkid,
        CourseGroupPkid = c.CourseGroupPkid,
        PublishStatusPkid = c.PublishStatusPkid,
        ScheduleOn = c.ScheduleOn,
        ScheduleOff = c.ScheduleOff,
        Hour = c.Hour,
        ListPrice = c.ListPrice,
        LearningCredit = c.LearningCredit,
        Material = c.Material,
        Objective = c.Objective,
        Target = c.Target,
        Prerequisites = c.Prerequisites,
        Outline = c.Outline,
        TowardCertOrExam = c.TowardCertOrExam,
        Note = c.Note,
        OtherInfo = c.OtherInfo,
        CanRepeat = c.CanRepeat,
        PartnerName = Partners.GetValueOrDefault(c.PartnerPkid),
        CourseGroupDescription = c.CourseGroupPkid is { } cg ? CourseGroups.GetValueOrDefault(cg) : null,
        PublishStatusDescription = PublishStatuses.GetValueOrDefault(c.PublishStatusPkid),
        CertificationPkids = [.. c.CertificationPkids],
        JobCategoryPkids = [.. c.JobCategoryPkids],
    };
}
