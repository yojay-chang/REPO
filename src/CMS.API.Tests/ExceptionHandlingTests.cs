using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Middleware;
using CMS.API.Models;
using CMS.API.Tests.Fakes;

namespace CMS.API.Tests;

/// <summary>
/// Verifies the global exception-handling middleware: an endpoint whose repository throws returns a
/// consistent 500 with a generic message and no leaked stack trace / SQL, while the meaningful status
/// codes that are set without throwing — 401 (unauthenticated), 403 (forbidden) and validation/400 —
/// are unchanged.
/// </summary>
public class ExceptionHandlingTests : IClassFixture<ExceptionHandlingApiFactory>
{
    private readonly ExceptionHandlingApiFactory _factory;
    private readonly HttpClient _client;

    public ExceptionHandlingTests(ExceptionHandlingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(string userId, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { UserId = userId, Password = password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    private HttpRequestMessage Authorized(HttpMethod method, string uri, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    [Fact]
    public async Task ThrowingEndpoint_Returns500_WithGenericMessage()
    {
        var token = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/app-roles", token));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(ExceptionHandlingMiddleware.GenericMessage, payload!.Message);
    }

    [Fact]
    public async Task ThrowingEndpoint_Response_NeverLeaksStackTraceOrSql()
    {
        var token = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/app-roles", token));
        var raw = await response.Content.ReadAsStringAsync();

        // None of the sensitive exception detail may cross the wire.
        Assert.DoesNotContain(ThrowingAppRoleRepository.SensitiveMessage, raw);
        Assert.DoesNotContain("SELECT", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", raw);
        Assert.DoesNotContain("at CMS.API", raw);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_StillReturns401_NotConvertedTo500()
    {
        var response = await _client.GetAsync("/api/app-roles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_AsNonAdmin_StillReturns403_NotConvertedTo500()
    {
        var ninaToken = await GetTokenAsync("nina", FakeAuthRepository.NinaPassword);

        var response = await _client.SendAsync(
            Authorized(HttpMethod.Post, "/api/Auth/reset-password", ninaToken, new { userId = "miles" }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidationError_StillReturns400_NotConvertedTo500()
    {
        var adminToken = await GetTokenAsync("helen", FakeAuthRepository.HelenPassword);

        var response = await _client.SendAsync(
            Authorized(HttpMethod.Post, "/api/Auth/reset-password", adminToken, new { userId = "   " }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record ErrorBody(string Message);
}
