using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.API.Tests;

/// <summary>
/// Covers <c>PUT /api/Auth/profile</c>: it updates the UserName of the JWT user only, ignores any
/// UserId sent in the body, requires a non-empty UserName, and is not reachable without a token.
/// Uses <see cref="AuthorizationApiFactory"/> so the real JWT enforcement is exercised end-to-end.
/// </summary>
public class ProfileEndpointTests : IClassFixture<AuthorizationApiFactory>
{
    private readonly AuthorizationApiFactory _factory;
    private readonly HttpClient _client;

    public ProfileEndpointTests(AuthorizationApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetHelenTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.HelenPassword });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    private async Task<HttpResponseMessage> PutProfileAsync(string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/Auth/profile")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private AuthCredential? GetCredential(string userId)
    {
        // The fake repository is a singleton within the factory — inspect its state directly.
        var repo = _factory.Services.GetRequiredService<IAuthRepository>();
        return repo.GetCredentialAsync(userId).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task UpdateProfile_UpdatesUserNameForJwtUser()
    {
        var token = await GetHelenTokenAsync();

        var response = await PutProfileAsync(token, new { userName = "Helen Renamed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("helen", body!.UserId);
        Assert.Equal("Helen Renamed", body.UserName);

        // Persisted against the JWT user.
        Assert.Equal("Helen Renamed", GetCredential("helen")!.UserName);
    }

    [Fact]
    public async Task UpdateProfile_IgnoresUserIdInBody()
    {
        var token = await GetHelenTokenAsync();
        var milesNameBefore = GetCredential("miles")!.UserName;

        // Try to hijack another account by sending a different UserId — it must be ignored.
        var response = await PutProfileAsync(token,
            new { userId = "miles", userName = "Hijacked Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        // The JWT user (helen) was changed, not the body's UserId (miles).
        Assert.Equal("helen", body!.UserId);
        Assert.Equal("Hijacked Name", GetCredential("helen")!.UserName);
        Assert.Equal(milesNameBefore, GetCredential("miles")!.UserName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProfile_EmptyOrWhitespaceUserName_ReturnsBadRequest(string userName)
    {
        var token = await GetHelenTokenAsync();
        var before = GetCredential("helen")!.UserName;

        var response = await PutProfileAsync(token, new { userName });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Unchanged.
        Assert.Equal(before, GetCredential("helen")!.UserName);
    }

    [Fact]
    public async Task UpdateProfile_TrimsUserName()
    {
        var token = await GetHelenTokenAsync();

        var response = await PutProfileAsync(token, new { userName = "  Spaced Name  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("Spaced Name", body!.UserName);
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/Auth/profile", new { userName = "Whatever" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
