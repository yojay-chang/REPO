using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

public class FakeLookupRepository : ILookupRepository
{
    public Task<IEnumerable<AppUserLookup>> GetAppUsersAsync()
        => Task.FromResult(new[]
        {
            new AppUserLookup { UserId = "helen", UserName = "helen" },
            new AppUserLookup { UserId = "Jenny_Tsao", UserName = "Jenny_Tsao" },
            new AppUserLookup { UserId = "miles", UserName = "Miles Sun" }
        }.AsEnumerable());

    public Task<IEnumerable<AppRoleLookup>> GetAppRolesAsync()
        => Task.FromResult(new[]
        {
            new AppRoleLookup { RoleId = "Admin", RoleName = "Administrator" },
            new AppRoleLookup { RoleId = "User", RoleName = "User" }
        }.AsEnumerable());

    public Task<IEnumerable<PublishStatusLookup>> GetPublishStatusesAsync()
        => Task.FromResult(new[]
        {
            new PublishStatusLookup { Pkid = 1, Description = "草稿" },
            new PublishStatusLookup { Pkid = 2, Description = "已發布" },
            new PublishStatusLookup { Pkid = 3, Description = "已停用" }
        }.AsEnumerable());

    public Task<IEnumerable<PartnerLookup>> GetPartnersAsync()
        => Task.FromResult(new[]
        {
            new PartnerLookup { Pkid = 1, Name = "微軟" },
            new PartnerLookup { Pkid = 2, Name = "思科" },
            new PartnerLookup { Pkid = 3, Name = "紅帽" }
        }.AsEnumerable());

    public Task<IEnumerable<CourseGroupLookup>> GetCourseGroupsAsync()
        => Task.FromResult(new[]
        {
            new CourseGroupLookup { Pkid = 1, Description = "資料庫" },
            new CourseGroupLookup { Pkid = 2, Description = "網路管理" },
            new CourseGroupLookup { Pkid = 3, Description = "雲端服務" }
        }.AsEnumerable());

    public Task<IEnumerable<CertificationLookup>> GetCertificationsAsync()
        => Task.FromResult(new[]
        {
            new CertificationLookup { Pkid = 1, Title = "MCSA" },
            new CertificationLookup { Pkid = 2, Title = "CCNA" },
            new CertificationLookup { Pkid = 3, Title = "RHCSA" }
        }.AsEnumerable());

    public Task<IEnumerable<JobCategoryLookup>> GetJobCategoriesAsync()
        => Task.FromResult(new[]
        {
            new JobCategoryLookup { Pkid = 1, Description = "系統工程師" },
            new JobCategoryLookup { Pkid = 2, Description = "網路工程師" },
            new JobCategoryLookup { Pkid = 3, Description = "雲端架構師" }
        }.AsEnumerable());

    public Task<IEnumerable<TrainingCenterLookup>> GetTrainingCentersAsync()
        => Task.FromResult(new[]
        {
            new TrainingCenterLookup { Pkid = 1, Name = "台北" },
            new TrainingCenterLookup { Pkid = 2, Name = "新竹" },
            new TrainingCenterLookup { Pkid = 3, Name = "台中" },
            new TrainingCenterLookup { Pkid = 4, Name = "高雄" },
            new TrainingCenterLookup { Pkid = 5, Name = "線上研討會" }
        }.AsEnumerable());

    public Task<IEnumerable<PromotionLookup>> GetPromotionsAsync()
        => Task.FromResult(new[]
        {
            new PromotionLookup { Pkid = 1, PromoCode = "20251204_SkillTrainAI" },
            new PromotionLookup { Pkid = 2, PromoCode = "251211_GoogleAI" },
            new PromotionLookup { Pkid = 3, PromoCode = "20251215_n8n" }
        }.AsEnumerable());
}
