using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class PublishStatusesControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public PublishStatusesControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededStatuses()
    {
        var statuses = await _client.GetFromJsonAsync<List<PublishStatus>>("/api/publish-statuses");

        Assert.NotNull(statuses);
        Assert.Contains(statuses!, s => s.Pkid == 1 && s.Description == "草稿");
        Assert.Contains(statuses!, s => s.Pkid == 2 && s.Description == "已發布");
    }

    // ---- List (query filter) ----

    [Fact]
    public async Task Query_ByKeyword_FiltersByDescription()
    {
        var response = await _client.PostAsJsonAsync("/api/publish-statuses/query",
            new PublishStatusQuery { Keyword = "已停用" });
        response.EnsureSuccessStatusCode();

        var statuses = await response.Content.ReadFromJsonAsync<List<PublishStatus>>();
        var status = Assert.Single(statuses!);
        Assert.Equal(3, status.Pkid);
    }

    [Fact]
    public async Task Query_ByIsPublished_FiltersResults()
    {
        var response = await _client.PostAsJsonAsync("/api/publish-statuses/query",
            new PublishStatusQuery { IsPublished = true });
        response.EnsureSuccessStatusCode();

        var statuses = await response.Content.ReadFromJsonAsync<List<PublishStatus>>();
        Assert.All(statuses!, s => Assert.True(s.IsPublished));
        Assert.Contains(statuses!, s => s.Pkid == 2);
        Assert.DoesNotContain(statuses!, s => s.Pkid == 1);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var response = await _client.PostAsJsonAsync("/api/publish-statuses/query", new PublishStatusQuery());
        response.EnsureSuccessStatusCode();

        var statuses = await response.Content.ReadFromJsonAsync<List<PublishStatus>>();
        Assert.True(statuses!.Count >= 3);
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsStatus()
    {
        var status = await _client.GetFromJsonAsync<PublishStatus>("/api/publish-statuses/1");

        Assert.NotNull(status);
        Assert.Equal("草稿", status!.Description);
        Assert.True(status.IsDraft);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/publish-statuses/99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewStatus_ReturnsCreatedAndPersists()
    {
        var request = new PublishStatusRequest
        {
            Pkid = 10,
            Description = "審核中",
            IsDraft = true,
            IsPublished = false,
            IsDiscontinued = false
        };

        var response = await _client.PostAsJsonAsync("/api/publish-statuses", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PublishStatus>();
        Assert.Equal((byte)10, created!.Pkid);

        var fetched = await _client.GetFromJsonAsync<PublishStatus>("/api/publish-statuses/10");
        Assert.Equal("審核中", fetched!.Description);
    }

    [Fact]
    public async Task Create_DuplicatePkid_ReturnsConflict()
    {
        var request = new PublishStatusRequest { Pkid = 1, Description = "Dup" };

        var response = await _client.PostAsJsonAsync("/api/publish-statuses", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingDescription_ReturnsBadRequest()
    {
        var request = new PublishStatusRequest { Pkid = 20, Description = "" };

        var response = await _client.PostAsJsonAsync("/api/publish-statuses", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingStatus_ChangesFields()
    {
        // Seed a dedicated status to mutate.
        await _client.PostAsJsonAsync("/api/publish-statuses", new PublishStatusRequest
        {
            Pkid = 30,
            Description = "before",
            IsDraft = true,
            IsPublished = false,
            IsDiscontinued = false
        });

        var update = new PublishStatusRequest
        {
            Pkid = 30,
            Description = "after",
            IsDraft = false,
            IsPublished = true,
            IsDiscontinued = false
        };

        var response = await _client.PutAsJsonAsync("/api/publish-statuses", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<PublishStatus>();
        Assert.Equal("after", updated!.Description);
        Assert.False(updated.IsDraft);
        Assert.True(updated.IsPublished);
    }

    [Fact]
    public async Task Update_MissingStatus_ReturnsNotFound()
    {
        var update = new PublishStatusRequest { Pkid = 200, Description = "Ghost" };

        var response = await _client.PutAsJsonAsync("/api/publish-statuses", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_ExistingStatus_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/publish-statuses", new PublishStatusRequest
        {
            Pkid = 40,
            Description = "臨時"
        });

        var response = await _client.DeleteAsync("/api/publish-statuses/40");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync("/api/publish-statuses/40");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingStatus_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/publish-statuses/201");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Lookup ----

    [Fact]
    public async Task GetPublishStatusesLookup_ReturnsStatuses()
    {
        var statuses = await _client.GetFromJsonAsync<List<PublishStatusLookup>>("/api/lookups/publish-statuses");

        Assert.NotNull(statuses);
        Assert.Contains(statuses!, s => s.Pkid == 1 && s.Description == "草稿");
    }
}
