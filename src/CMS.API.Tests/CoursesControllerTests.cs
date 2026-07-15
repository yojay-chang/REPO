using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class CoursesControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public CoursesControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CourseRequest NewRequest() => new()
    {
        Title = "Kubernetes 實戰",
        OfficialTitle = "Certified Kubernetes Administrator",
        CourseId = "CKA-100",
        ProdCourseId = "PRD-CKA",
        FriendlyUrl = "cka-100",
        DisplayOrder = 5,
        PartnerPkid = 3,
        CourseGroupPkid = 3,
        PublishStatusPkid = 2,
        ScheduleOn = new DateOnly(2026, 3, 1),
        ScheduleOff = new DateOnly(2036, 3, 1),
        Hour = 35,
        ListPrice = 28000m,
        LearningCredit = 4.5m,
        CanRepeat = true,
        CertificationPkids = [3],
        JobCategoryPkids = [1, 3],
    };

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededCoursesWithFkLabels()
    {
        var courses = await _client.GetFromJsonAsync<List<Course>>("/api/courses");

        Assert.NotNull(courses);
        var azure = Assert.Single(courses!, c => c.Pkid == 1);
        Assert.Equal("Azure 基礎", azure.Title);
        Assert.Equal("微軟", azure.PartnerName);
        Assert.Equal("雲端服務", azure.CourseGroupDescription);
        Assert.Equal("已發布", azure.PublishStatusDescription);
    }

    [Fact]
    public async Task GetAll_IsOrderedByDisplayOrder()
    {
        var courses = await _client.GetFromJsonAsync<List<Course>>("/api/courses");

        Assert.NotNull(courses);
        var orders = courses!.Select(c => c.DisplayOrder).ToList();
        Assert.Equal(orders.OrderBy(o => o), orders);
    }

    [Fact]
    public async Task GetAll_NullableCourseGroup_HasNullDescription()
    {
        var courses = await _client.GetFromJsonAsync<List<Course>>("/api/courses");

        var ccna = Assert.Single(courses!, c => c.Pkid == 2);
        Assert.Null(ccna.CourseGroupPkid);
        Assert.Null(ccna.CourseGroupDescription);
    }

    // ---- List (query filters) ----

    [Fact]
    public async Task Query_ByKeyword_MatchesCourseId()
    {
        var response = await _client.PostAsJsonAsync("/api/courses/query",
            new CourseQuery { Keyword = "CCNA" });
        response.EnsureSuccessStatusCode();

        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();
        var course = Assert.Single(courses!);
        Assert.Equal(2, course.Pkid);
    }

    [Fact]
    public async Task Query_ByPartner_FiltersByPartnerPkid()
    {
        var response = await _client.PostAsJsonAsync("/api/courses/query",
            new CourseQuery { PartnerPkid = 1 });
        response.EnsureSuccessStatusCode();

        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();
        Assert.All(courses!, c => Assert.Equal(1, c.PartnerPkid));
    }

    [Fact]
    public async Task Query_ByCanRepeat_FiltersByFlag()
    {
        var response = await _client.PostAsJsonAsync("/api/courses/query",
            new CourseQuery { CanRepeat = false });
        response.EnsureSuccessStatusCode();

        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();
        Assert.All(courses!, c => Assert.False(c.CanRepeat));
        Assert.Contains(courses!, c => c.Pkid == 2);
    }

    [Fact]
    public async Task Query_ByScheduleOnRange_FiltersByDate()
    {
        var response = await _client.PostAsJsonAsync("/api/courses/query",
            new CourseQuery { ScheduleOnFrom = new DateOnly(2026, 1, 15), ScheduleOnTo = new DateOnly(2026, 12, 31) });
        response.EnsureSuccessStatusCode();

        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();
        Assert.Contains(courses!, c => c.Pkid == 2);
        Assert.DoesNotContain(courses!, c => c.Pkid == 1);
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsCourseWithNnLists()
    {
        var course = await _client.GetFromJsonAsync<Course>("/api/courses/1");

        Assert.NotNull(course);
        Assert.Equal("Azure 基礎", course!.Title);
        Assert.Equal([1], course.CertificationPkids);
        Assert.Equal([(short)1], course.JobCategoryPkids);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/courses/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewCourse_ReturnsCreatedAndPersistsNn()
    {
        var response = await _client.PostAsJsonAsync("/api/courses", NewRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Course>();
        Assert.NotNull(created);
        Assert.True(created!.Pkid > 0);
        Assert.Equal("Kubernetes 實戰", created.Title);
        Assert.Equal("紅帽", created.PartnerName);

        var fetched = await _client.GetFromJsonAsync<Course>($"/api/courses/{created.Pkid}");
        Assert.Equal([3], fetched!.CertificationPkids);
        Assert.Equal([(short)1, (short)3], fetched.JobCategoryPkids);
    }

    [Fact]
    public async Task Create_MissingTitle_ReturnsBadRequest()
    {
        var request = NewRequest();
        request.Title = "";

        var response = await _client.PostAsJsonAsync("/api/courses", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingCourseId_ReturnsBadRequest()
    {
        var request = NewRequest();
        request.CourseId = "";

        var response = await _client.PostAsJsonAsync("/api/courses", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingCourse_ChangesFieldsAndNn()
    {
        var seed = await _client.PostAsJsonAsync("/api/courses", NewRequest());
        var created = await seed.Content.ReadFromJsonAsync<Course>();

        var update = NewRequest();
        update.Pkid = created!.Pkid;
        update.Title = "Kubernetes 進階";
        update.CertificationPkids = [];
        update.JobCategoryPkids = [2];

        var response = await _client.PutAsJsonAsync("/api/courses", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<Course>();
        Assert.Equal("Kubernetes 進階", updated!.Title);
        Assert.Empty(updated.CertificationPkids);
        Assert.Equal([(short)2], updated.JobCategoryPkids);
    }

    [Fact]
    public async Task Update_MissingCourse_ReturnsNotFound()
    {
        var update = NewRequest();
        update.Pkid = 777;

        var response = await _client.PutAsJsonAsync("/api/courses", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingTitle_ReturnsBadRequest()
    {
        var update = NewRequest();
        update.Pkid = 1;
        update.Title = "";

        var response = await _client.PutAsJsonAsync("/api/courses", update);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_ExistingCourse_ReturnsNoContent()
    {
        var seed = await _client.PostAsJsonAsync("/api/courses", NewRequest());
        var created = await seed.Content.ReadFromJsonAsync<Course>();

        var response = await _client.DeleteAsync($"/api/courses/{created!.Pkid}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync($"/api/courses/{created.Pkid}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingCourse_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/courses/888");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Lookups ----

    [Fact]
    public async Task GetCertificationsLookup_ReturnsRows()
    {
        var certifications = await _client.GetFromJsonAsync<List<CertificationLookup>>("/api/lookups/certifications");

        Assert.NotNull(certifications);
        Assert.Contains(certifications!, c => c.Pkid == 1 && c.Title == "MCSA");
    }

    [Fact]
    public async Task GetJobCategoriesLookup_ReturnsRows()
    {
        var jobCategories = await _client.GetFromJsonAsync<List<JobCategoryLookup>>("/api/lookups/job-categories");

        Assert.NotNull(jobCategories);
        Assert.Contains(jobCategories!, j => j.Pkid == 2 && j.Description == "網路工程師");
    }
}
