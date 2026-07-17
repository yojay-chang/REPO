using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Models;
using CMS.API.Tests.Fakes;

namespace CMS.API.Tests;

/// <summary>
/// Verifies the global JWT authorization policy: every controller requires an authenticated user
/// except <c>AuthController</c>, which stays anonymous.
/// </summary>
public class AuthorizationTests : IClassFixture<AuthorizationApiFactory>
{
    private readonly HttpClient _client;

    public AuthorizationTests(AuthorizationApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/app-users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/app-users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        var token = await GetAccessTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/app-users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthController_Login_IsAnonymous()
    {
        // No Authorization header — login must succeed anyway.
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.HelenPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthController_Login_WithBadCredentials_Returns401NotChallenge()
    {
        // Still anonymous — the 401 comes from the credential check, and the endpoint is reachable.
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid credentials", body);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.HelenPassword });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }
}
