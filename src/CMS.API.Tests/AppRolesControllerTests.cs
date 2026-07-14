using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class AppRolesControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public AppRolesControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededRoles()
    {
        var roles = await _client.GetFromJsonAsync<List<AppRole>>("/api/app-roles");

        Assert.NotNull(roles);
        Assert.Contains(roles!, r => r.RoleId == "Admin");
        Assert.Contains(roles!, r => r.RoleId == "User");
    }

    [Fact]
    public async Task GetAll_IncludesUserCount()
    {
        var roles = await _client.GetFromJsonAsync<List<AppRole>>("/api/app-roles");

        var admin = Assert.Single(roles!, r => r.RoleId == "Admin");
        Assert.Equal(3, admin.UserCount);
    }

    // ---- List (query filter) ----

    [Fact]
    public async Task Query_ByKeyword_FiltersByRoleName()
    {
        var response = await _client.PostAsJsonAsync("/api/app-roles/query",
            new AppRoleQuery { Keyword = "Administrator" });
        response.EnsureSuccessStatusCode();

        var roles = await response.Content.ReadFromJsonAsync<List<AppRole>>();
        var role = Assert.Single(roles!);
        Assert.Equal("Admin", role.RoleId);
    }

    [Fact]
    public async Task Query_ByPermissionLevelRange_FiltersResults()
    {
        var response = await _client.PostAsJsonAsync("/api/app-roles/query",
            new AppRoleQuery { PermissionLevelFrom = 50, PermissionLevelTo = 200 });
        response.EnsureSuccessStatusCode();

        var roles = await response.Content.ReadFromJsonAsync<List<AppRole>>();
        Assert.All(roles!, r => Assert.InRange(r.PermissionLevel, 50, 200));
        Assert.Contains(roles!, r => r.RoleId == "User");
        Assert.DoesNotContain(roles!, r => r.RoleId == "Admin");
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var response = await _client.PostAsJsonAsync("/api/app-roles/query", new AppRoleQuery());
        response.EnsureSuccessStatusCode();

        var roles = await response.Content.ReadFromJsonAsync<List<AppRole>>();
        Assert.True(roles!.Count >= 2);
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsRoleWithUserIds()
    {
        var role = await _client.GetFromJsonAsync<AppRole>("/api/app-roles/Admin");

        Assert.NotNull(role);
        Assert.Equal("Administrator", role!.RoleName);
        Assert.Equal(1, role.PermissionLevel);
        Assert.Equal(3, role.UserIds.Count);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/app-roles/DoesNotExist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewRole_ReturnsCreatedAndPersists()
    {
        var request = new AppRoleRequest
        {
            RoleId = "Editor",
            RoleName = "Content Editor",
            PermissionLevel = 50,
            Description = "內容編輯",
            UserIds = ["helen"]
        };

        var response = await _client.PostAsJsonAsync("/api/app-roles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<AppRole>();
        Assert.Equal("Editor", created!.RoleId);
        Assert.Single(created.UserIds);

        var fetched = await _client.GetFromJsonAsync<AppRole>("/api/app-roles/Editor");
        Assert.Equal("Content Editor", fetched!.RoleName);
    }

    [Fact]
    public async Task Create_DuplicateRoleId_ReturnsConflict()
    {
        var request = new AppRoleRequest { RoleId = "Admin", RoleName = "Dup" };

        var response = await _client.PostAsJsonAsync("/api/app-roles", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRoleId_ReturnsBadRequest()
    {
        var request = new AppRoleRequest { RoleId = "", RoleName = "No Id" };

        var response = await _client.PostAsJsonAsync("/api/app-roles", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingRole_ChangesFieldsAndUsers()
    {
        // Seed a dedicated role to mutate.
        await _client.PostAsJsonAsync("/api/app-roles", new AppRoleRequest
        {
            RoleId = "Temp",
            RoleName = "Temp Role",
            PermissionLevel = 80,
            Description = "before",
            UserIds = ["helen", "miles"]
        });

        var update = new AppRoleRequest
        {
            RoleId = "Temp",
            RoleName = "Temp Role Updated",
            PermissionLevel = 90,
            Description = "after",
            UserIds = ["miles"]
        };

        var response = await _client.PutAsJsonAsync("/api/app-roles", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<AppRole>();
        Assert.Equal("Temp Role Updated", updated!.RoleName);
        Assert.Equal(90, updated.PermissionLevel);
        Assert.Equal("after", updated.Description);
        Assert.Single(updated.UserIds);
        Assert.Equal("miles", updated.UserIds[0]);
    }

    [Fact]
    public async Task Update_MissingRole_ReturnsNotFound()
    {
        var update = new AppRoleRequest { RoleId = "Ghost", RoleName = "Ghost" };

        var response = await _client.PutAsJsonAsync("/api/app-roles", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Lookup ----

    [Fact]
    public async Task GetAppUsersLookup_ReturnsUsers()
    {
        var users = await _client.GetFromJsonAsync<List<AppUserLookup>>("/api/lookups/app-users");

        Assert.NotNull(users);
        Assert.Contains(users!, u => u.UserId == "helen");
    }
}
