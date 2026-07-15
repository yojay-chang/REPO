using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.API.Tests;

/// <summary>
/// Covers <c>POST /api/Auth/reset-password</c> (Admin-only). A fresh factory is created per test (not a
/// shared <c>IClassFixture</c>) because a successful reset mutates a user's stored password. Real JWT
/// authentication and role authorization are exercised end-to-end.
/// </summary>
public class ResetPasswordEndpointTests : IDisposable
{
    private readonly AuthorizationApiFactory _factory = new();
    private readonly HttpClient _client;

    public ResetPasswordEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> GetTokenAsync(string userId, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = userId, Password = password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    private async Task<HttpResponseMessage> ResetPasswordAsync(string? token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/reset-password")
        {
            Content = JsonContent.Create(body),
        };
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private AuthCredential GetCredential(string userId)
    {
        // The fake repository is a singleton within the factory — inspect its state directly.
        var repo = _factory.Services.GetRequiredService<IAuthRepository>();
        return repo.GetCredentialAsync(userId).GetAwaiter().GetResult()!;
    }

    // ---- Access control ----

    [Fact]
    public async Task ResetPassword_WithoutToken_Returns401()
    {
        var response = await ResetPasswordAsync(token: null, new { userId = "miles" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_AsNonAdmin_Returns403AndChangesNothing()
    {
        var ninaToken = await GetTokenAsync("nina", FakeAuthRepository.NinaPassword);
        var beforeHash = GetCredential("miles").PasswordHash;

        var response = await ResetPasswordAsync(ninaToken, new { userId = "miles" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(beforeHash, GetCredential("miles").PasswordHash);
    }

    // ---- Admin reset sets the hash to SHA256(default) and stamps the timestamp ----

    [Fact]
    public async Task ResetPassword_AsAdmin_SetsHashToSha256OfDefaultAndUpdatesTimestamp()
    {
        var adminToken = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);
        // Capture values (not the live reference) — the fake mutates the object it returns.
        var before = GetCredential("miles");
        var beforeHash = before.PasswordHash;
        var beforeTime = before.PasswordUpdatedTime;

        var response = await ResetPasswordAsync(adminToken, new { userId = "miles" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = GetCredential("miles");
        Assert.Equal(PasswordHasher.Sha256(FakeAuthRepository.DefaultPassword), after.PasswordHash);
        Assert.NotEqual(beforeHash, after.PasswordHash);
        Assert.NotNull(after.PasswordUpdatedTime);
        Assert.True(after.PasswordUpdatedTime > beforeTime);
    }

    [Fact]
    public async Task ResetPassword_AsAdmin_Response_NeverContainsPasswordOrHash()
    {
        var adminToken = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);

        var response = await ResetPasswordAsync(adminToken, new { userId = "miles" });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PasswordHasher.Sha256(FakeAuthRepository.DefaultPassword), raw);
        Assert.DoesNotContain(FakeAuthRepository.DefaultPassword, raw);
    }

    // ---- Validation ----

    [Fact]
    public async Task ResetPassword_UnknownUser_Returns404()
    {
        var adminToken = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);

        var response = await ResetPasswordAsync(adminToken, new { userId = "ghost" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_MissingUserId_Returns400()
    {
        var adminToken = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);

        var response = await ResetPasswordAsync(adminToken, new { userId = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
