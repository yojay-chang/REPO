using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class CourseGroupsControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public CourseGroupsControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededCourseGroups()
    {
        var courseGroups = await _client.GetFromJsonAsync<List<CourseGroup>>("/api/course-groups");

        Assert.NotNull(courseGroups);
        Assert.Contains(courseGroups!, cg => cg.Pkid == 1 && cg.Description == "資料庫");
        Assert.Contains(courseGroups!, cg => cg.Pkid == 2 && cg.Description == "網路管理");
    }

    [Fact]
    public async Task GetAll_IsOrderedByPkidDescending()
    {
        var courseGroups = await _client.GetFromJsonAsync<List<CourseGroup>>("/api/course-groups");

        Assert.NotNull(courseGroups);
        var pkids = courseGroups!.Select(cg => cg.Pkid).ToList();
        Assert.Equal(pkids.OrderByDescending(p => p), pkids);
    }

    // ---- List (query filter) ----

    [Fact]
    public async Task Query_ByKeyword_FiltersByDescription()
    {
        var response = await _client.PostAsJsonAsync("/api/course-groups/query",
            new CourseGroupQuery { Keyword = "網路" });
        response.EnsureSuccessStatusCode();

        var courseGroups = await response.Content.ReadFromJsonAsync<List<CourseGroup>>();
        var courseGroup = Assert.Single(courseGroups!);
        Assert.Equal(2, courseGroup.Pkid);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var response = await _client.PostAsJsonAsync("/api/course-groups/query", new CourseGroupQuery());
        response.EnsureSuccessStatusCode();

        var courseGroups = await response.Content.ReadFromJsonAsync<List<CourseGroup>>();
        Assert.True(courseGroups!.Count >= 3);
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsCourseGroup()
    {
        var courseGroup = await _client.GetFromJsonAsync<CourseGroup>("/api/course-groups/1");

        Assert.NotNull(courseGroup);
        Assert.Equal("資料庫", courseGroup!.Description);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/course-groups/99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewCourseGroup_ReturnsCreatedAndPersists()
    {
        var request = new CourseGroupRequest { Description = "資訊安全" };

        var response = await _client.PostAsJsonAsync("/api/course-groups", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CourseGroup>();
        Assert.NotNull(created);
        Assert.True(created!.Pkid > 0);
        Assert.Equal("資訊安全", created.Description);

        var fetched = await _client.GetFromJsonAsync<CourseGroup>($"/api/course-groups/{created.Pkid}");
        Assert.Equal("資訊安全", fetched!.Description);
    }

    [Fact]
    public async Task Create_MissingDescription_ReturnsBadRequest()
    {
        var request = new CourseGroupRequest { Description = "" };

        var response = await _client.PostAsJsonAsync("/api/course-groups", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingCourseGroup_ChangesFields()
    {
        // Seed a dedicated course group to mutate.
        var seed = await _client.PostAsJsonAsync("/api/course-groups",
            new CourseGroupRequest { Description = "before" });
        var created = await seed.Content.ReadFromJsonAsync<CourseGroup>();

        var update = new CourseGroupRequest
        {
            Pkid = created!.Pkid,
            Description = "after"
        };

        var response = await _client.PutAsJsonAsync("/api/course-groups", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<CourseGroup>();
        Assert.Equal("after", updated!.Description);
    }

    [Fact]
    public async Task Update_MissingCourseGroup_ReturnsNotFound()
    {
        var update = new CourseGroupRequest
        {
            Pkid = 200,
            Description = "Ghost"
        };

        var response = await _client.PutAsJsonAsync("/api/course-groups", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingDescription_ReturnsBadRequest()
    {
        var update = new CourseGroupRequest
        {
            Pkid = 1,
            Description = ""
        };

        var response = await _client.PutAsJsonAsync("/api/course-groups", update);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_ExistingCourseGroup_ReturnsNoContent()
    {
        var seed = await _client.PostAsJsonAsync("/api/course-groups",
            new CourseGroupRequest { Description = "臨時群組" });
        var created = await seed.Content.ReadFromJsonAsync<CourseGroup>();

        var response = await _client.DeleteAsync($"/api/course-groups/{created!.Pkid}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync($"/api/course-groups/{created.Pkid}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingCourseGroup_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/course-groups/201");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Lookup ----

    [Fact]
    public async Task GetCourseGroupsLookup_ReturnsCourseGroups()
    {
        var courseGroups = await _client.GetFromJsonAsync<List<CourseGroupLookup>>("/api/lookups/course-groups");

        Assert.NotNull(courseGroups);
        Assert.Contains(courseGroups!, cg => cg.Pkid == 1 && cg.Description == "資料庫");
    }
}
