using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class PartnersControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public PartnersControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededPartners()
    {
        var partners = await _client.GetFromJsonAsync<List<Partner>>("/api/partners");

        Assert.NotNull(partners);
        Assert.Contains(partners!, p => p.Pkid == 1 && p.Name == "微軟");
        Assert.Contains(partners!, p => p.Pkid == 2 && p.Name == "思科");
    }

    [Fact]
    public async Task GetAll_IsOrderedByDisplayOrder()
    {
        var partners = await _client.GetFromJsonAsync<List<Partner>>("/api/partners");

        Assert.NotNull(partners);
        var orders = partners!.Select(p => p.DisplayOrder).ToList();
        Assert.Equal(orders.OrderBy(o => o), orders);
    }

    // ---- List (query filter) ----

    [Fact]
    public async Task Query_ByKeyword_FiltersByName()
    {
        var response = await _client.PostAsJsonAsync("/api/partners/query",
            new PartnerQuery { Keyword = "思科" });
        response.EnsureSuccessStatusCode();

        var partners = await response.Content.ReadFromJsonAsync<List<Partner>>();
        var partner = Assert.Single(partners!);
        Assert.Equal(2, partner.Pkid);
    }

    [Fact]
    public async Task Query_ByKeyword_MatchesAppKey()
    {
        var response = await _client.PostAsJsonAsync("/api/partners/query",
            new PartnerQuery { Keyword = "CISCO" });
        response.EnsureSuccessStatusCode();

        var partners = await response.Content.ReadFromJsonAsync<List<Partner>>();
        Assert.Contains(partners!, p => p.Pkid == 2);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var response = await _client.PostAsJsonAsync("/api/partners/query", new PartnerQuery());
        response.EnsureSuccessStatusCode();

        var partners = await response.Content.ReadFromJsonAsync<List<Partner>>();
        Assert.True(partners!.Count >= 3);
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsPartner()
    {
        var partner = await _client.GetFromJsonAsync<Partner>("/api/partners/1");

        Assert.NotNull(partner);
        Assert.Equal("微軟", partner!.Name);
        Assert.Equal("MS", partner.AppKey);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/partners/99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewPartner_ReturnsCreatedAndPersists()
    {
        var request = new PartnerRequest
        {
            Name = "甲骨文",
            AppKey = "ORCL",
            NameOnPartnerMenu = "甲骨文資料庫課程",
            NameOnCourseDetailPage = "甲骨文",
            DisplayOrder = 10,
            ImageFilename = "oracle.png"
        };

        var response = await _client.PostAsJsonAsync("/api/partners", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Partner>();
        Assert.NotNull(created);
        Assert.True(created!.Pkid > 0);
        Assert.Equal("甲骨文", created.Name);

        var fetched = await _client.GetFromJsonAsync<Partner>($"/api/partners/{created.Pkid}");
        Assert.Equal("甲骨文", fetched!.Name);
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var request = new PartnerRequest
        {
            Name = "",
            AppKey = "X",
            NameOnPartnerMenu = "選單",
            NameOnCourseDetailPage = "頁面",
            DisplayOrder = 1
        };

        var response = await _client.PostAsJsonAsync("/api/partners", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingAppKey_ReturnsBadRequest()
    {
        var request = new PartnerRequest
        {
            Name = "測試",
            AppKey = "",
            NameOnPartnerMenu = "選單",
            NameOnCourseDetailPage = "頁面",
            DisplayOrder = 1
        };

        var response = await _client.PostAsJsonAsync("/api/partners", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingPartner_ChangesFields()
    {
        // Seed a dedicated partner to mutate.
        var seed = await _client.PostAsJsonAsync("/api/partners", new PartnerRequest
        {
            Name = "before",
            AppKey = "BF",
            NameOnPartnerMenu = "before menu",
            NameOnCourseDetailPage = "before page",
            DisplayOrder = 20
        });
        var created = await seed.Content.ReadFromJsonAsync<Partner>();

        var update = new PartnerRequest
        {
            Pkid = created!.Pkid,
            Name = "after",
            AppKey = "AF",
            NameOnPartnerMenu = "after menu",
            NameOnCourseDetailPage = "after page",
            DisplayOrder = 21,
            ImageFilename = "after.png"
        };

        var response = await _client.PutAsJsonAsync("/api/partners", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<Partner>();
        Assert.Equal("after", updated!.Name);
        Assert.Equal("AF", updated.AppKey);
        Assert.Equal("after.png", updated.ImageFilename);
    }

    [Fact]
    public async Task Update_MissingPartner_ReturnsNotFound()
    {
        var update = new PartnerRequest
        {
            Pkid = 200,
            Name = "Ghost",
            AppKey = "GH",
            NameOnPartnerMenu = "menu",
            NameOnCourseDetailPage = "page",
            DisplayOrder = 1
        };

        var response = await _client.PutAsJsonAsync("/api/partners", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingName_ReturnsBadRequest()
    {
        var update = new PartnerRequest
        {
            Pkid = 1,
            Name = "",
            AppKey = "MS",
            NameOnPartnerMenu = "menu",
            NameOnCourseDetailPage = "page",
            DisplayOrder = 1
        };

        var response = await _client.PutAsJsonAsync("/api/partners", update);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_ExistingPartner_ReturnsNoContent()
    {
        var seed = await _client.PostAsJsonAsync("/api/partners", new PartnerRequest
        {
            Name = "臨時廠商",
            AppKey = "TMP",
            NameOnPartnerMenu = "臨時",
            NameOnCourseDetailPage = "臨時",
            DisplayOrder = 30
        });
        var created = await seed.Content.ReadFromJsonAsync<Partner>();

        var response = await _client.DeleteAsync($"/api/partners/{created!.Pkid}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync($"/api/partners/{created.Pkid}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingPartner_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/partners/201");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Lookup ----

    [Fact]
    public async Task GetPartnersLookup_ReturnsPartners()
    {
        var partners = await _client.GetFromJsonAsync<List<PartnerLookup>>("/api/lookups/partners");

        Assert.NotNull(partners);
        Assert.Contains(partners!, p => p.Pkid == 1 && p.Name == "微軟");
    }
}
