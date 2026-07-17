using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LookupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<AppUserLookup>> GetAppUsersAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppUserLookup>(
            "SELECT UserId, UserName FROM AppUser ORDER BY UserName ASC");
    }

    public async Task<IEnumerable<AppRoleLookup>> GetAppRolesAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppRoleLookup>(
            "SELECT RoleId, RoleName FROM AppRole ORDER BY RoleName ASC");
    }

    public async Task<IEnumerable<PublishStatusLookup>> GetPublishStatusesAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<PublishStatusLookup>(
            "SELECT pkid, Description FROM PublishStatus ORDER BY pkid ASC");
    }

    public async Task<IEnumerable<PartnerLookup>> GetPartnersAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<PartnerLookup>(
            "SELECT pkid, Name FROM Partner ORDER BY DisplayOrder ASC");
    }

    public async Task<IEnumerable<CourseGroupLookup>> GetCourseGroupsAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<CourseGroupLookup>(
            "SELECT pkid, Description FROM CourseGroup ORDER BY pkid ASC");
    }

    public async Task<IEnumerable<CertificationLookup>> GetCertificationsAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        // Title is nchar(100) → RTRIM to drop the fixed-width padding.
        return await db.QueryAsync<CertificationLookup>(
            "SELECT pkid, RTRIM(Title) AS Title FROM Certification ORDER BY pkid ASC");
    }

    public async Task<IEnumerable<JobCategoryLookup>> GetJobCategoriesAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<JobCategoryLookup>(
            "SELECT pkid, Description FROM JobCategory ORDER BY pkid ASC");
    }

    public async Task<IEnumerable<TrainingCenterLookup>> GetTrainingCentersAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<TrainingCenterLookup>(
            "SELECT pkid, Name FROM TrainingCenter ORDER BY DisplayOrder ASC");
    }

    public async Task<IEnumerable<PromotionLookup>> GetPromotionsAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<PromotionLookup>(
            "SELECT pkid, PromoCode FROM Promotion2 ORDER BY PromoCode ASC");
    }
}
