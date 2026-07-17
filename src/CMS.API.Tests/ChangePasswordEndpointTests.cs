using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Auth;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.API.Tests;

/// <summary>
/// Covers <c>POST /api/Auth/change-password</c>. A fresh factory is created per test (not a shared
/// <c>IClassFixture</c>) because a successful change mutates helen's stored password, which would
/// otherwise break the login other tests rely on. Real JWT enforcement is exercised end-to-end.
/// </summary>
public class ChangePasswordEndpointTests : IDisposable
{
    private readonly AuthorizationApiFactory _factory = new();
    private readonly HttpClient _client;

    public ChangePasswordEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> GetHelenTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.HelenPassword });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
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

    private AuthCredential GetHelen()
    {
        // The fake repository is a singleton within the factory — inspect its state directly.
        var repo = _factory.Services.GetRequiredService<IAuthRepository>();
        return repo.GetCredentialAsync("helen").GetAwaiter().GetResult()!;
    }

    // ---- 1. Wrong current password changes nothing ----

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ChangesNothing()
    {
        var token = await GetHelenTokenAsync();
        // Capture values, not the live reference (the fake returns the object it would mutate).
        var before = GetHelen();
        var beforeHash = before.PasswordHash;
        var beforeTime = before.PasswordUpdatedTime;

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = "not-the-current-password",
            newPassword = "NewPass1!",
            confirmNewPassword = "NewPass1!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var after = GetHelen();
        Assert.Equal(beforeHash, after.PasswordHash);
        Assert.Equal(beforeTime, after.PasswordUpdatedTime);
    }

    // ---- 2. Complexity is enforced ----

    [Theory]
    [InlineData("Ab1!")]          // valid classes but too short (< 8)
    [InlineData("alllowercase")]  // long enough but only 1 class
    [InlineData("lowercase123")]  // long enough but only 2 classes (lower + digit)
    [InlineData("PASSWORD1")]     // only 2 classes (upper + digit)
    public async Task ChangePassword_WeakNewPassword_IsRejectedWithComplexityMessage(string weak)
    {
        var token = await GetHelenTokenAsync();
        var beforeHash = GetHelen().PasswordHash;

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.HelenPassword,
            newPassword = weak,
            confirmNewPassword = weak,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(PasswordPolicy.ComplexityMessage, body);

        // Nothing changed.
        Assert.Equal(beforeHash, GetHelen().PasswordHash);
    }

    [Theory]
    [InlineData("Abcdef1!")]      // 4 classes
    [InlineData("Abcdefg1")]      // 3 classes (upper + lower + digit)
    [InlineData("abcdefg1!")]     // 3 classes (lower + digit + symbol)
    public async Task ChangePassword_MeetsComplexity_IsAccepted(string strong)
    {
        var token = await GetHelenTokenAsync();

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.HelenPassword,
            newPassword = strong,
            confirmNewPassword = strong,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PasswordHasher.Sha256(strong), GetHelen().PasswordHash);
    }

    // ---- 3. New / confirm mismatch is rejected ----

    [Fact]
    public async Task ChangePassword_NewAndConfirmMismatch_IsRejected()
    {
        var token = await GetHelenTokenAsync();
        var beforeHash = GetHelen().PasswordHash;

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.HelenPassword,
            newPassword = "NewPass1!",
            confirmNewPassword = "DifferentPass1!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Nothing changed.
        Assert.Equal(beforeHash, GetHelen().PasswordHash);
    }

    // ---- 4. A valid change updates the hash and the timestamp ----

    [Fact]
    public async Task ChangePassword_Valid_SetsHashToSha256OfNewAndUpdatesTimestamp()
    {
        var token = await GetHelenTokenAsync();
        // Capture the values (not the live reference) — the fake returns the same object it mutates.
        var before = GetHelen();
        var beforeHash = before.PasswordHash;
        var beforeTime = before.PasswordUpdatedTime;
        const string newPassword = "Str0ng!Pass";

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.HelenPassword,
            newPassword,
            confirmNewPassword = newPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = GetHelen();
        Assert.Equal(PasswordHasher.Sha256(newPassword), after.PasswordHash);
        Assert.NotEqual(beforeHash, after.PasswordHash);
        Assert.NotNull(after.PasswordUpdatedTime);
        Assert.True(after.PasswordUpdatedTime > beforeTime);

        // The new password now authenticates; the old one no longer does.
        var oldLogin = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.HelenPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    // ---- Never leaks a hash; requires a token ----

    [Fact]
    public async Task ChangePassword_Response_NeverContainsPasswordHash()
    {
        var token = await GetHelenTokenAsync();
        const string newPassword = "Str0ng!Pass";

        var response = await ChangePasswordAsync(token, new
        {
            currentPassword = FakeAuthRepository.HelenPassword,
            newPassword,
            confirmNewPassword = newPassword,
        });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PasswordHasher.Sha256(newPassword), raw);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/change-password", new
        {
            currentPassword = FakeAuthRepository.HelenPassword,
            newPassword = "NewPass1!",
            confirmNewPassword = "NewPass1!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
