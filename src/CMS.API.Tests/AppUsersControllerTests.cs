using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class AppUsersControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public AppUsersControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededUsers()
    {
        var users = await _client.GetFromJsonAsync<List<AppUser>>("/api/app-users");

        Assert.NotNull(users);
        Assert.Contains(users!, u => u.UserId == "helen");
        Assert.Contains(users!, u => u.UserId == "miles");
    }

    [Fact]
    public async Task GetAll_IncludesRoleCount()
    {
        var users = await _client.GetFromJsonAsync<List<AppUser>>("/api/app-users");

        var helen = Assert.Single(users!, u => u.UserId == "helen");
        Assert.Equal(2, helen.RoleCount);
    }

    // ---- List (query filter) ----

    [Fact]
    public async Task Query_ByKeyword_FiltersByUserName()
    {
        var response = await _client.PostAsJsonAsync("/api/app-users/query",
            new AppUserQuery { Keyword = "Helen" });
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();
        var user = Assert.Single(users!);
        Assert.Equal("helen", user.UserId);
    }

    [Fact]
    public async Task Query_ByIsActive_FiltersResults()
    {
        var response = await _client.PostAsJsonAsync("/api/app-users/query",
            new AppUserQuery { IsActive = false });
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();
        Assert.All(users!, u => Assert.False(u.IsActive));
        Assert.Contains(users!, u => u.UserId == "miles");
    }

    [Fact]
    public async Task Query_ByRoleId_ReturnsAssignedUsers()
    {
        var response = await _client.PostAsJsonAsync("/api/app-users/query",
            new AppUserQuery { RoleId = "Admin" });
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();
        Assert.Contains(users!, u => u.UserId == "helen");
        Assert.DoesNotContain(users!, u => u.UserId == "miles");
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsUserWithRoleIds()
    {
        var user = await _client.GetFromJsonAsync<AppUser>("/api/app-users/helen");

        Assert.NotNull(user);
        Assert.Equal("Helen Wang", user!.UserName);
        Assert.True(user.IsActive);
        Assert.Equal(2, user.RoleIds.Count);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/app-users/DoesNotExist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewUser_ReturnsCreatedAndPersists()
    {
        var request = new AppUserRequest
        {
            UserId = "jenny",
            UserName = "Jenny Tsao",
            IsActive = true,
            RoleIds = ["User"]
        };

        var response = await _client.PostAsJsonAsync("/api/app-users", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<AppUser>();
        Assert.Equal("jenny", created!.UserId);
        Assert.Single(created.RoleIds);

        var fetched = await _client.GetFromJsonAsync<AppUser>("/api/app-users/jenny");
        Assert.Equal("Jenny Tsao", fetched!.UserName);
    }

    [Fact]
    public async Task Create_DuplicateUserId_ReturnsConflict()
    {
        var request = new AppUserRequest { UserId = "helen", UserName = "Dup" };

        var response = await _client.PostAsJsonAsync("/api/app-users", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingUserId_ReturnsBadRequest()
    {
        var request = new AppUserRequest { UserId = "", UserName = "No Id" };

        var response = await _client.PostAsJsonAsync("/api/app-users", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingUserName_ReturnsBadRequest()
    {
        var request = new AppUserRequest { UserId = "nameless", UserName = "" };

        var response = await _client.PostAsJsonAsync("/api/app-users", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingUser_ChangesFieldsAndRoles()
    {
        await _client.PostAsJsonAsync("/api/app-users", new AppUserRequest
        {
            UserId = "temp",
            UserName = "Temp User",
            IsActive = true,
            RoleIds = ["Admin", "User"]
        });

        var update = new AppUserRequest
        {
            UserId = "temp",
            UserName = "Temp User Updated",
            IsActive = false,
            RoleIds = ["User"]
        };

        var response = await _client.PutAsJsonAsync("/api/app-users", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<AppUser>();
        Assert.Equal("Temp User Updated", updated!.UserName);
        Assert.False(updated.IsActive);
        Assert.Single(updated.RoleIds);
        Assert.Equal("User", updated.RoleIds[0]);
    }

    [Fact]
    public async Task Update_MissingUser_ReturnsNotFound()
    {
        var update = new AppUserRequest { UserId = "ghost", UserName = "Ghost" };

        var response = await _client.PutAsJsonAsync("/api/app-users", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_ExistingUser_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/app-users", new AppUserRequest
        {
            UserId = "todelete",
            UserName = "To Delete",
            IsActive = true
        });

        var response = await _client.DeleteAsync("/api/app-users/todelete");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync("/api/app-users/todelete");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingUser_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/app-users/DoesNotExist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Password reset moved to POST /api/Auth/reset-password (Admin-only) — see ResetPasswordEndpointTests.

    // ---- Lookup ----

    [Fact]
    public async Task GetAppRolesLookup_ReturnsRoles()
    {
        var roles = await _client.GetFromJsonAsync<List<AppRoleLookup>>("/api/lookups/app-roles");

        Assert.NotNull(roles);
        Assert.Contains(roles!, r => r.RoleId == "Admin");
    }
}
