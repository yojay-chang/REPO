using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Tests.Fakes;

namespace CMS.API.Tests;

public class AuthControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<HttpResponseMessage> Login(string userId, string password) =>
        await _client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { UserId = userId, Password = password });

    // ---- Success ----

    [Fact]
    public async Task Login_ValidActiveUser_ReturnsProfileWithToken()
    {
        var response = await Login("helen", FakeAuthRepository.HelenPassword);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.Equal("helen", body!.UserId);
        Assert.Equal("Helen Wang", body.UserName);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    // ---- Failure cases: all return a generic 401 ----

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var response = await Login("helen", "wrong-password");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUserId_ReturnsUnauthorized()
    {
        var response = await Login("nobody", FakeAuthRepository.HelenPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsUnauthorized()
    {
        // miles has the correct password but IsActive = 0.
        var response = await Login("miles", FakeAuthRepository.MilesPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Failure_DoesNotRevealWhichCheckFailed()
    {
        var wrongPassword = await (await Login("helen", "wrong-password")).Content.ReadAsStringAsync();
        var unknownUser = await (await Login("nobody", "whatever")).Content.ReadAsStringAsync();

        // Same generic message regardless of the failing check.
        Assert.Contains("invalid credentials", wrongPassword);
        Assert.Contains("invalid credentials", unknownUser);
    }

    // ---- JWT contents ----

    [Fact]
    public async Task Login_Token_CarriesRoleClaims()
    {
        var body = await (await Login("helen", FakeAuthRepository.HelenPassword))
            .Content.ReadFromJsonAsync<LoginResponse>();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        var roles = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
        Assert.Equal(2, roles.Count);
    }

    [Fact]
    public async Task Login_Token_CarriesUserIdAndUserNameClaims()
    {
        var body = await (await Login("helen", FakeAuthRepository.HelenPassword))
            .Content.ReadFromJsonAsync<LoginResponse>();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        Assert.Equal("helen", token.Claims.Single(c => c.Type == "userId").Value);
        Assert.Equal("Helen Wang", token.Claims.Single(c => c.Type == "userName").Value);
    }

    [Fact]
    public async Task Login_Token_ExpiresInAbout24Hours()
    {
        var body = await (await Login("helen", FakeAuthRepository.HelenPassword))
            .Content.ReadFromJsonAsync<LoginResponse>();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        var hoursUntilExpiry = (token.ValidTo - DateTime.UtcNow).TotalHours;
        Assert.InRange(hoursUntilExpiry, 23.9, 24.1);
    }

    // ---- PasswordHash must never leak ----

    [Fact]
    public async Task Login_Response_NeverContainsPasswordHash()
    {
        var response = await Login("helen", FakeAuthRepository.HelenPassword);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PasswordHasher.Sha256(FakeAuthRepository.HelenPassword), raw);
    }
}
