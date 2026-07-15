using System.Data;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CourseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // FK labels are JOINed in as flat read-only columns (Partner INNER, CourseGroup LEFT — nullable,
    // PublishStatus INNER). Single-type mapping — no multi-map / splitOn required.
    private const string SelectColumns = @"
        SELECT c.pkid, c.Title, c.OfficialTitle, c.CourseId, c.ProdCourseId, c.FriendlyUrl,
               c.DisplayOrder, c.Partner_pkid AS PartnerPkid, c.CourseGroup_pkid AS CourseGroupPkid,
               c.PublishStatus_pkid AS PublishStatusPkid, c.ScheduleOn, c.ScheduleOff, c.Hour,
               c.ListPrice, c.LearningCredit, c.Material, c.Objective, c.Target, c.Prerequisites,
               c.Outline, c.TowardCertOrExam, c.Note, c.OtherInfo, c.CanRepeat,
               p.Name AS PartnerName, cg.Description AS CourseGroupDescription,
               ps.Description AS PublishStatusDescription
        FROM Course c
             INNER JOIN Partner p        ON p.pkid  = c.Partner_pkid
             LEFT  JOIN CourseGroup cg   ON cg.pkid = c.CourseGroup_pkid
             INNER JOIN PublishStatus ps ON ps.pkid = c.PublishStatus_pkid";

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<Course>($"{SelectColumns} ORDER BY c.DisplayOrder ASC");
    }

    public async Task<IEnumerable<Course>> QueryAsync(CourseQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add(@"(c.Title LIKE @Keyword
                        OR c.OfficialTitle LIKE @Keyword
                        OR c.CourseId LIKE @Keyword
                        OR c.ProdCourseId LIKE @Keyword
                        OR c.FriendlyUrl LIKE @Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        if (query.PartnerPkid.HasValue)
        {
            where.Add("c.Partner_pkid = @PartnerPkid");
            parameters.Add("PartnerPkid", query.PartnerPkid.Value);
        }

        if (query.CourseGroupPkid.HasValue)
        {
            where.Add("c.CourseGroup_pkid = @CourseGroupPkid");
            parameters.Add("CourseGroupPkid", query.CourseGroupPkid.Value);
        }

        if (query.PublishStatusPkid.HasValue)
        {
            where.Add("c.PublishStatus_pkid = @PublishStatusPkid");
            parameters.Add("PublishStatusPkid", query.PublishStatusPkid.Value);
        }

        if (query.ScheduleOnFrom.HasValue)
        {
            where.Add("c.ScheduleOn >= @ScheduleOnFrom");
            parameters.Add("ScheduleOnFrom", query.ScheduleOnFrom.Value);
        }

        if (query.ScheduleOnTo.HasValue)
        {
            where.Add("c.ScheduleOn <= @ScheduleOnTo");
            parameters.Add("ScheduleOnTo", query.ScheduleOnTo.Value);
        }

        if (query.ScheduleOffFrom.HasValue)
        {
            where.Add("c.ScheduleOff >= @ScheduleOffFrom");
            parameters.Add("ScheduleOffFrom", query.ScheduleOffFrom.Value);
        }

        if (query.ScheduleOffTo.HasValue)
        {
            where.Add("c.ScheduleOff <= @ScheduleOffTo");
            parameters.Add("ScheduleOffTo", query.ScheduleOffTo.Value);
        }

        if (query.CanRepeat.HasValue)
        {
            where.Add("c.CanRepeat = @CanRepeat");
            parameters.Add("CanRepeat", query.CanRepeat.Value);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY c.DisplayOrder ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<Course>(sql, parameters);
    }

    public async Task<Course?> GetByIdAsync(int pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        var course = await db.QuerySingleOrDefaultAsync<Course>(
            $"{SelectColumns} WHERE c.pkid = @Pkid", new { Pkid = pkid });

        if (course is null)
            return null;

        var certificationPkids = await db.QueryAsync<int>(
            "SELECT Certification_pkid FROM CourseInCertification WHERE Course_pkid = @Pkid ORDER BY Certification_pkid",
            new { Pkid = pkid });
        course.CertificationPkids = certificationPkids.ToList();

        var jobCategoryPkids = await db.QueryAsync<short>(
            "SELECT JobCategory_pkid FROM CourseJobCategories WHERE Course_pkid = @Pkid ORDER BY JobCategory_pkid",
            new { Pkid = pkid });
        course.JobCategoryPkids = jobCategoryPkids.ToList();

        return course;
    }

    public async Task<int> CreateAsync(CourseRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var pkid = await db.ExecuteScalarAsync<int>(
            @"INSERT INTO Course (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
                  Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff, Hour,
                  ListPrice, LearningCredit, Material, Objective, Target, Prerequisites, Outline,
                  TowardCertOrExam, Note, OtherInfo, CanRepeat)
              VALUES (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
                  @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff, @Hour,
                  @ListPrice, @LearningCredit, @Material, @Objective, @Target, @Prerequisites, @Outline,
                  @TowardCertOrExam, @Note, @OtherInfo, @CanRepeat);
              SELECT CAST(SCOPE_IDENTITY() AS int);",
            Params(request), tx);

        await SyncCertificationsAsync(db, tx, pkid, request.CertificationPkids);
        await SyncJobCategoriesAsync(db, tx, pkid, request.JobCategoryPkids);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(CourseRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var affected = await db.ExecuteAsync(
            @"UPDATE Course
                 SET Title = @Title, OfficialTitle = @OfficialTitle, CourseId = @CourseId,
                     ProdCourseId = @ProdCourseId, FriendlyUrl = @FriendlyUrl, DisplayOrder = @DisplayOrder,
                     Partner_pkid = @PartnerPkid, CourseGroup_pkid = @CourseGroupPkid,
                     PublishStatus_pkid = @PublishStatusPkid, ScheduleOn = @ScheduleOn, ScheduleOff = @ScheduleOff,
                     Hour = @Hour, ListPrice = @ListPrice, LearningCredit = @LearningCredit, Material = @Material,
                     Objective = @Objective, Target = @Target, Prerequisites = @Prerequisites, Outline = @Outline,
                     TowardCertOrExam = @TowardCertOrExam, Note = @Note, OtherInfo = @OtherInfo, CanRepeat = @CanRepeat
               WHERE pkid = @Pkid",
            Params(request), tx);

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncCertificationsAsync(db, tx, request.Pkid, request.CertificationPkids);
        await SyncJobCategoriesAsync(db, tx, request.Pkid, request.JobCategoryPkids);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(int pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        await db.ExecuteAsync("DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid", new { Pkid = pkid }, tx);
        await db.ExecuteAsync("DELETE FROM CourseJobCategories WHERE Course_pkid = @Pkid", new { Pkid = pkid }, tx);
        var affected = await db.ExecuteAsync("DELETE FROM Course WHERE pkid = @Pkid", new { Pkid = pkid }, tx);

        tx.Commit();
        return affected > 0;
    }

    /// <summary>Scalar parameters shared by INSERT and UPDATE (excludes the N-N lists).</summary>
    private static object Params(CourseRequest r) => new
    {
        r.Pkid,
        r.Title,
        r.OfficialTitle,
        r.CourseId,
        r.ProdCourseId,
        r.FriendlyUrl,
        r.DisplayOrder,
        r.PartnerPkid,
        r.CourseGroupPkid,
        r.PublishStatusPkid,
        r.ScheduleOn,
        r.ScheduleOff,
        r.Hour,
        r.ListPrice,
        r.LearningCredit,
        r.Material,
        r.Objective,
        r.Target,
        r.Prerequisites,
        r.Outline,
        r.TowardCertOrExam,
        r.Note,
        r.OtherInfo,
        r.CanRepeat
    };

    /// <summary>Delete-then-reinsert the CourseInCertification rows for a course (n-n sync).</summary>
    private static async Task SyncCertificationsAsync(IDbConnection db, IDbTransaction tx, int coursePkid, List<int> certificationPkids)
    {
        await db.ExecuteAsync("DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid", new { Pkid = coursePkid }, tx);

        var distinct = certificationPkids.Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await db.ExecuteAsync(
            "INSERT INTO CourseInCertification (Course_pkid, Certification_pkid) VALUES (@CoursePkid, @CertificationPkid)",
            distinct.Select(id => new { CoursePkid = coursePkid, CertificationPkid = id }), tx);
    }

    /// <summary>Delete-then-reinsert the CourseJobCategories rows for a course (n-n sync).</summary>
    private static async Task SyncJobCategoriesAsync(IDbConnection db, IDbTransaction tx, int coursePkid, List<short> jobCategoryPkids)
    {
        await db.ExecuteAsync("DELETE FROM CourseJobCategories WHERE Course_pkid = @Pkid", new { Pkid = coursePkid }, tx);

        var distinct = jobCategoryPkids.Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await db.ExecuteAsync(
            "INSERT INTO CourseJobCategories (Course_pkid, JobCategory_pkid) VALUES (@CoursePkid, @JobCategoryPkid)",
            distinct.Select(id => new { CoursePkid = coursePkid, JobCategoryPkid = id }), tx);
    }
}
