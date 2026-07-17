using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Auth;
using CMS.API.Data;
using CMS.API.Middleware;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.API.Tests;

/// <summary>
/// Covers the "first login on the default password must change it" rule end-to-end: the login flag +
/// restricting claim, the middleware that enforces it server-side, and the fresh token that releases it.
/// A fresh factory per test — a successful change mutates dana's stored password in the fake repository.
/// </summary>
public class ForcedPasswordChangeTests : IDisposable
{
    private readonly AuthorizationApiFactory _factory = new();
    private readonly HttpClient _client;

    /// <summary>Satisfies PasswordPolicy and differs from the default — a valid replacement.</summary>
    private const string StrongNewPassword = "Str0ng!Pass";

    public ForcedPasswordChangeTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<LoginResponse> LoginAsync(string userId, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = userId, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    /// <summary>Log in as the user who is still on the system default password.</summary>
    private Task<LoginResponse> LoginAsDefaultPasswordUserAsync()
        => LoginAsync("dana", FakeAuthRepository.DefaultPassword);

    private async Task<HttpResponseMessage> GetAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ChangePasswordAsync(string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/change-password")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private AuthCredential GetDana()
    {
        var repo = _factory.Services.GetRequiredService<IAuthRepository>();
        return repo.GetCredentialAsync("dana").GetAwaiter().GetResult()!;
    }

    // ---- 1. Login flags the default-password account (and only it) ----

    [Fact]
    public async Task Login_OnDefaultPassword_FlagsMustChangePassword()
    {
        var body = await LoginAsDefaultPasswordUserAsync();

        Assert.True(body.MustChangePassword);
        Assert.NotEmpty(body.AccessToken); // still issued — it is restricted, not withheld
    }

    [Fact]
    public async Task Login_OnOwnPassword_DoesNotFlagMustChangePassword()
    {
        var body = await LoginAsync("helen", FakeAuthRepository.HelenPassword);

        Assert.False(body.MustChangePassword);
    }

    // ---- 2. The restricted token is refused everywhere but the change-password flow ----

    [Fact]
    public async Task RestrictedToken_OnAnyOtherEndpoint_Returns403WithCode()
    {
        var token = (await LoginAsDefaultPasswordUserAsync()).AccessToken;

        var response = await GetAsync("/api/app-roles", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains(PasswordChangeRequiredMiddleware.ErrorCode, raw);
    }

    [Fact]
    public async Task RestrictedToken_CanStillReachChangePassword()
    {
        var token = (await LoginAsDefaultPasswordUserAsync()).AccessToken;

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.DefaultPassword,
            newPassword = StrongNewPassword,
            confirmNewPassword = StrongNewPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NormalToken_IsUnaffectedByTheMiddleware()
    {
        var token = (await LoginAsync("helen", FakeAuthRepository.HelenPassword)).AccessToken;

        var response = await GetAsync("/api/app-roles", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- 3. Changing the password releases the restriction ----

    [Fact]
    public async Task ChangePassword_ReturnsFreshTokenThatUnlocksTheApi()
    {
        var token = (await LoginAsDefaultPasswordUserAsync()).AccessToken;

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.DefaultPassword,
            newPassword = StrongNewPassword,
            confirmNewPassword = StrongNewPassword,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var newToken = (await response.Content.ReadFromJsonAsync<ChangePasswordResponseBody>())!.AccessToken;
        Assert.False(string.IsNullOrEmpty(newToken));
        Assert.NotEqual(token, newToken);

        // The fresh token carries no restriction — the API is now reachable.
        Assert.Equal(HttpStatusCode.OK, (await GetAsync("/api/app-roles", newToken)).StatusCode);

        // And a subsequent login is no longer flagged.
        Assert.False((await LoginAsync("dana", StrongNewPassword)).MustChangePassword);
    }

    [Fact]
    public async Task ChangePassword_RejectsReusingTheSystemDefault()
    {
        var token = (await LoginAsDefaultPasswordUserAsync()).AccessToken;
        var beforeHash = GetDana().PasswordHash;

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.DefaultPassword,
            newPassword = FakeAuthRepository.DefaultPassword,
            confirmNewPassword = FakeAuthRepository.DefaultPassword,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(beforeHash, GetDana().PasswordHash);
    }

    // ---- 4. An Admin reset puts the target back into the forced-change state ----

    [Fact]
    public async Task AfterAdminReset_TargetsNextLogin_MustChangePassword()
    {
        // helen (Admin) resets nina, whose own password then becomes the system default.
        var adminToken = (await LoginAsync("helen", FakeAuthRepository.HelenPassword)).AccessToken;
        var reset = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/reset-password")
        {
            Content = JsonContent.Create(new { userId = "nina" }),
        };
        reset.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(reset)).StatusCode);

        var body = await LoginAsync("nina", FakeAuthRepository.DefaultPassword);

        Assert.True(body.MustChangePassword);
    }

    /// <summary>Shape of the change-password 200 body (message + the replacement token).</summary>
    private sealed class ChangePasswordResponseBody
    {
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }
}
